using FieldReport.Core.Data;
using FieldReport.Core.Models;

namespace FieldReport.Core.Services;

/// <summary>Zgłoszenia przykładowe trzymane w pamięci (aplikacja nie ma jeszcze bazy ani API).</summary>
public sealed class SampleReportService : IReportService
{
    private readonly IReadOnlyList<Report> _reports = SampleReports.Create(DateTimeOffset.Now);

    public Task<Result<IReadOnlyList<Report>>> GetAllAsync(CancellationToken ct = default)
    {
        IReadOnlyList<Report> sorted = _reports.OrderByDescending(r => r.UpdatedAt).ToList();
        return Task.FromResult(Result<IReadOnlyList<Report>>.Success(sorted));
    }
}
