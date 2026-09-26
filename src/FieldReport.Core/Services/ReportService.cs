using FieldReport.Core.Data;
using FieldReport.Core.Models;

namespace FieldReport.Core.Services;

/// <summary>Operacje na zgłoszeniach: reguły biznesowe + zapis w repozytorium. ViewModel dostaje Result.</summary>
public sealed class ReportService(IReportRepository repository) : IReportService
{
    public async Task<Result<IReadOnlyList<Report>>> GetAllAsync(CancellationToken ct = default)
    {
        var reports = await repository.GetAllAsync(ct);
        // Od najnowszej aktualizacji; przy równych datach decyduje data utworzenia, a potem Id, więc kolejność się nie zmienia
        IReadOnlyList<Report> sorted = reports.OrderByDescending(r => r.UpdatedAt).ThenByDescending(r => r.CreatedAt)
            .ThenBy(r => r.Id).ToList();
        return Result<IReadOnlyList<Report>>.Success(sorted);
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var report = await repository.GetAsync(id, ct);
        if (report is null) return Result.Failure(Errors.ReportNotFound);
        if (report.SyncState != SyncState.Pending) return Result.Failure(Errors.ReportAlreadySynced); // wysłanych nie usuwamy

        await repository.DeleteAsync(id, ct);
        return Result.Success();
    }
}
