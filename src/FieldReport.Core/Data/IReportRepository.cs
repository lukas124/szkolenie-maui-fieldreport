using FieldReport.Core.Models;

namespace FieldReport.Core.Data;

/// <summary>
/// Lokalny magazyn zgłoszeń. Implementacje (in-memory, SQLite) zachowują się tak samo (LSP),
/// więc podmiana to zmiana jednej linii w <c>AddCoreServices()</c>.
/// </summary>
public interface IReportRepository
{
    Task<IReadOnlyList<Report>> GetAllAsync(CancellationToken ct = default);
    Task<Report?> GetAsync(Guid id, CancellationToken ct = default);
    Task SaveAsync(Report report, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
