using FieldReport.Core.Data;
using FieldReport.Core.Models;

namespace FieldReport.Tests.TestData;

/// <summary>Repozytorium w pamięci do testów: zachowuje się jak lokalna baza, ale bez pliku SQLite.</summary>
public sealed class InMemoryReportRepository : IReportRepository
{
    private readonly Dictionary<Guid, Report> _reports = new();

    public Task<IReadOnlyList<Report>> GetAllAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<Report>>(_reports.Values.OrderByDescending(r => r.UpdatedAt).ToList());

    public Task<Report?> GetAsync(Guid id, CancellationToken ct = default) =>
        Task.FromResult(_reports.GetValueOrDefault(id));

    public Task SaveAsync(Report report, CancellationToken ct = default)
    {
        _reports[report.Id] = report;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        _reports.Remove(id);
        return Task.CompletedTask;
    }

    public Task ReplaceAsync(Guid oldId, Report report, CancellationToken ct = default)
    {
        _reports.Remove(oldId);
        _reports[report.Id] = report;
        return Task.CompletedTask;
    }

    public Task DeleteAllAsync(CancellationToken ct = default)
    {
        _reports.Clear();
        return Task.CompletedTask;
    }
}
