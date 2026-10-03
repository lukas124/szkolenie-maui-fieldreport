using FieldReport.Core.Models;

namespace FieldReport.Core.Services;

/// <summary>Operacje na zgłoszeniach (lista i szczegóły). ViewModel reaguje na <see cref="Result"/>, nie zna źródła danych.</summary>
public interface IReportService
{
    /// <summary>Wszystkie zgłoszenia, od najnowszej aktualizacji.</summary>
    Task<Result<IReadOnlyList<Report>>> GetAllAsync(CancellationToken ct = default);
}
