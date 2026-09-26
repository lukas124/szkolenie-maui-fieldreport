using FieldReport.Core.Models;

namespace FieldReport.Core.Services;

/// <summary>
/// Lista zgłoszeń do wyświetlenia. <see cref="IsOffline"/> = serwer nieosiągalny: lista zawiera tylko zgłoszenia
/// czekające w telefonie na wysłanie.
/// </summary>
public sealed record ReportList(IReadOnlyList<Report> Reports, bool IsOffline);

/// <summary>Operacje na zgłoszeniach (lista, szczegóły, kreator nowego zgłoszenia). ViewModel reaguje na wynik, nie wie, skąd pochodzą dane.</summary>
public interface IReportService
{
    /// <summary>Zgłoszenia z API + niewysłane z telefonu, od najnowszej aktualizacji.</summary>
    Task<ReportList> GetAllAsync(CancellationToken ct = default);

    /// <summary>Jedno zgłoszenie: najpierw z telefonu (niewysłane), potem z API.</summary>
    Task<Result<Report>> GetAsync(Guid id, CancellationToken ct = default);

    /// <summary>Zapis nowego zgłoszenia z kreatora: status Nowe, „Oczekuje na wysłanie”. Zwraca nadany Guid.</summary>
    Task<Result<Guid>> CreateAsync(NewReportDraft draft, CancellationToken ct = default);

    /// <summary>
    /// Zmienia status (reguła w <see cref="Report.ChangeStatus"/>) i zapisuje zmianę w telefonie do wysłania.
    /// Przyjmuje całe zgłoszenie (to z ekranu szczegółów), więc zmiana działa także offline.
    /// </summary>
    Task<Result<Report>> ChangeStatusAsync(Report report, ReportStatus next, CancellationToken ct = default);

    /// <summary>Usuwa zgłoszenie z telefonu. Reguła: tylko nigdy niewysłane (<see cref="Report.CanDelete"/>).</summary>
    Task<Result> DeleteAsync(Guid id, CancellationToken ct = default);
}
