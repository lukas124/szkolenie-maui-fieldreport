using FieldReport.Core.Data;
using FieldReport.Core.Data.Api;
using FieldReport.Core.Models;
using FieldReport.Core.Services.Sync;

namespace FieldReport.Core.Services;

/// <summary>
/// Operacje na zgłoszeniach. Odczyt: zgłoszenia z API + niewysłane z telefonu (SQLite).
/// Zapis (nowe zgłoszenie, zmiana statusu): najpierw do telefonu jako „Oczekuje na wysłanie”, potem synchronizacja
/// w tle (SyncService wysyła i usuwa z telefonu). Dzięki temu zapis działa także bez internetu.
/// </summary>
public sealed class ReportService(
    IReportRepository local, IReportsApiClient api, IConnectivityService connectivity, IClock clock, ISyncScheduler sync,
    IPhotoStorage photos) : IReportService
{
    public async Task<ReportList> GetAllAsync(CancellationToken ct = default)
    {
        // bez sieci nie czekamy na timeout: tylko niewysłane (SQLite)
        if (!connectivity.IsOnline) return new ReportList(Sort(await local.GetAllAsync(ct)), IsOffline: true);

        // Najpierw serwer, potem telefon: wysyłka w tle może skończyć się w trakcie odczytu. W tej kolejności świeżo
        // wysłane zgłoszenie w najgorszym razie pojawi się dopiero po kolejnym odświeżeniu
        var server = await api.GetReportsAsync(ct); // GET api/reports
        var pending = await local.GetAllAsync(ct);  // niewysłane (SQLite)
        // offline: tylko niewysłane
        if (!server.IsSuccess) return new ReportList(Sort(pending), IsOffline: true);

        // Zgłoszenie z lokalną zmianą (np. statusu) pokazujemy w wersji z telefonu — jest nowsza niż ta na serwerze
        var pendingIds = pending.Select(r => r.Id).ToHashSet();
        var fromServer = server.Value!.Where(dto => !pendingIds.Contains(dto.Id)).Select(ReportDtoMapper.ToModel);
        return new ReportList(Sort(pending.Concat(fromServer)), IsOffline: false);
    }

    public async Task<Result<Report>> GetAsync(Guid id, CancellationToken ct = default)
    {
        var report = await local.GetAsync(id, ct);
        if (report is not null) return Result<Report>.Success(report);
        if (!connectivity.IsOnline) return Result<Report>.Failure(Errors.Network);

        var server = await api.GetReportAsync(id, ct);
        if (server.IsSuccess) return Result<Report>.Success(ReportDtoMapper.ToModel(server.Value!));
        return Result<Report>.Failure(server.Error == Errors.NotFound ? Errors.ReportNotFound : server.Error!);
    }

    public async Task<Result<Guid>> CreateAsync(NewReportDraft draft, CancellationToken ct = default)
    {
        if (draft.Title is null) return Result<Guid>.Failure(Errors.InvalidTitle);
        var description = string.IsNullOrWhiteSpace(draft.Description) ? null : draft.Description.Trim();
        if (description?.Length > Report.MaxDescriptionLength) return Result<Guid>.Failure(Errors.InvalidDescription);

        // lokalne Id (na serwerze Id nada API); CreateNew tworzy też klucz idempotencji do wysyłki (POST)
        var report = Report.CreateNew(Guid.NewGuid(), draft.Title, description, draft.Priority, clock.Now, draft.PhotoPath);
        await local.SaveAsync(report, ct);
        sync.RequestSync(); // kreator nowego zgłoszenia: zapis w telefonie → wysyłka w tle (lista wraca od razu)
        return Result<Guid>.Success(report.Id);
    }

    // Dostaje zgłoszenie wyświetlane w szczegółach (z telefonu albo z API), więc działa też bez sieci
    public async Task<Result<Report>> ChangeStatusAsync(Report report, ReportStatus next, CancellationToken ct = default)
    {
        var changed = report.ChangeStatus(next, clock.Now); // reguła przejść jest w modelu
        if (!changed.IsSuccess) return Result<Report>.Failure(changed.Error!);

        await local.SaveAsync(report, ct); // zmiana czeka w telefonie (działa też offline)
        sync.RequestSync();
        return Result<Report>.Success(report);
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var report = await local.GetAsync(id, ct);
        if (report is null) return Result.Failure(Errors.ReportNotFound);
        if (!report.CanDelete) return Result.Failure(Errors.ReportAlreadySynced); // wysłanych (także ze zmianami do wysłania) nie usuwamy

        await local.DeleteAsync(id, ct);
        if (report.PhotoPath is not null) await photos.DeleteAsync(report.PhotoPath, ct);
        return Result.Success();
    }

    // Od najnowszej aktualizacji; przy równych datach decyduje data utworzenia, a potem Id, więc kolejność się nie zmienia
    private static IReadOnlyList<Report> Sort(IEnumerable<Report> reports) =>
        reports.OrderByDescending(r => r.UpdatedAt).ThenByDescending(r => r.CreatedAt).ThenBy(r => r.Id).ToList();
}
