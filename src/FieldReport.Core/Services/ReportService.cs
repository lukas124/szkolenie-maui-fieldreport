using FieldReport.Core.Data;
using FieldReport.Core.Models;

namespace FieldReport.Core.Services;

/// <summary>Operacje na zgłoszeniach: reguły biznesowe + zapis w repozytorium. ViewModel dostaje Result.</summary>
public sealed class ReportService(IReportRepository repository, IClock clock) : IReportService
{
    public async Task<Result<IReadOnlyList<Report>>> GetAllAsync(CancellationToken ct = default)
    {
        var reports = await repository.GetAllAsync(ct);
        // Od najnowszej aktualizacji; przy równych datach decyduje data utworzenia, a potem Id, więc kolejność się nie zmienia
        IReadOnlyList<Report> sorted = reports.OrderByDescending(r => r.UpdatedAt).ThenByDescending(r => r.CreatedAt)
            .ThenBy(r => r.Id).ToList();
        return Result<IReadOnlyList<Report>>.Success(sorted);
    }

    public async Task<Result<Report>> GetAsync(Guid id, CancellationToken ct = default)
    {
        var report = await repository.GetAsync(id, ct);
        return report is null ? Result<Report>.Failure(Errors.ReportNotFound) : Result<Report>.Success(report);
    }

    public async Task<Result<Report>> ChangeStatusAsync(Guid id, ReportStatus next, CancellationToken ct = default)
    {
        var report = await repository.GetAsync(id, ct);
        if (report is null) return Result<Report>.Failure(Errors.ReportNotFound);

        var changed = report.ChangeStatus(next, clock.Now); // usługa: model (reguła) + zapis
        if (!changed.IsSuccess) return Result<Report>.Failure(changed.Error!);

        await repository.SaveAsync(report, ct);
        return Result<Report>.Success(report);
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var report = await repository.GetAsync(id, ct);
        if (report is null) return Result.Failure(Errors.ReportNotFound);
        if (!report.CanDelete) return Result.Failure(Errors.ReportAlreadySynced); // wysłanych (także ze zmianami do wysłania) nie usuwamy

        await repository.DeleteAsync(id, ct);
        return Result.Success();
    }
}
