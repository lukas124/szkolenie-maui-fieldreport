using FieldReport.Core.Models;

namespace FieldReport.Core.Services;

/// <summary>Operacje na zgłoszeniach (lista, szczegóły, kreator nowego zgłoszenia). ViewModel reaguje na <see cref="Result"/>, nie zna źródła danych.</summary>
public interface IReportService
{
    /// <summary>Wszystkie zgłoszenia, od najnowszej aktualizacji.</summary>
    Task<Result<IReadOnlyList<Report>>> GetAllAsync(CancellationToken ct = default);

    Task<Result<Report>> GetAsync(Guid id, CancellationToken ct = default);

    /// <summary>Zapis nowego zgłoszenia z kreatora: status Nowe, „Oczekuje na wysłanie”. Zwraca nadany Guid.</summary>
    Task<Result<Guid>> CreateAsync(NewReportDraft draft, CancellationToken ct = default);

    /// <summary>Zmienia status (reguła w <see cref="Report.ChangeStatus"/>) i zapisuje lokalnie.</summary>
    Task<Result<Report>> ChangeStatusAsync(Guid id, ReportStatus next, CancellationToken ct = default);

    /// <summary>Usuwa zgłoszenie. Reguła: tylko nigdy niewysłane (<see cref="Report.CanDelete"/>).</summary>
    Task<Result> DeleteAsync(Guid id, CancellationToken ct = default);
}
