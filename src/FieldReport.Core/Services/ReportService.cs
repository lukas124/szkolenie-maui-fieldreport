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

    // TODO: brak zgłoszenia → Errors.ReportNotFound; wysłane (SyncState != Pending) → Errors.ReportAlreadySynced;
    // w przeciwnym razie repository.DeleteAsync i Result.Success()
    public Task<Result> DeleteAsync(Guid id, CancellationToken ct = default) => throw new NotImplementedException();
}
