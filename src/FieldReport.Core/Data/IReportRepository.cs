using FieldReport.Core.Models;

namespace FieldReport.Core.Data;

/// <summary>
/// Lokalny magazyn zgłoszeń czekających na wysłanie (nowe zgłoszenia i zmiany statusu). Po udanej wysyłce
/// zgłoszenie znika z telefonu. Implementacje (in-memory, SQLite) zachowują się tak samo (LSP),
/// więc podmiana to zmiana jednej linii w <c>AddCoreServices()</c>.
/// </summary>
public interface IReportRepository
{
    Task<IReadOnlyList<Report>> GetAllAsync(CancellationToken ct = default);
    Task<Report?> GetAsync(Guid id, CancellationToken ct = default);
    Task SaveAsync(Report report, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);

    /// <summary>Wysyłka: zgłoszenie dostało Id z serwera — usuwa wiersz <paramref name="oldId"/> i zapisuje <paramref name="report"/> naraz.</summary>
    Task ReplaceAsync(Guid oldId, Report report, CancellationToken ct = default);

    /// <summary>Wylogowanie: usuwa wszystkie lokalne zgłoszenia.</summary>
    Task DeleteAllAsync(CancellationToken ct = default);
}
