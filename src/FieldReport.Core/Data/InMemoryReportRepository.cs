using System.Collections.Concurrent;
using FieldReport.Core.Models;
using FieldReport.Core.Services;

namespace FieldReport.Core.Data;

/// <summary>Repozytorium w pamięci z danymi przykładowymi (znikają po zamknięciu aplikacji). Singleton: jedna lista na całą aplikację.</summary>
public sealed class InMemoryReportRepository(IClock clock) : IReportRepository
{
    private readonly ConcurrentDictionary<Guid, Report> _reports =
        new(SampleReports.Create(clock.Now).Select(r => KeyValuePair.Create(r.Id, r)));

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
        _reports.TryRemove(id, out _);
        return Task.CompletedTask;
    }
}
