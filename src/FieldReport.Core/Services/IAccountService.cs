using FieldReport.Core.Models;

namespace FieldReport.Core.Services;

/// <summary>Konto technika w Ustawieniach: kim jestem + wylogowanie jako proces (kilka kroków = usługa, nie ViewModel).</summary>
public interface IAccountService
{
    Task<Session?> GetSessionAsync(CancellationToken ct = default);

    /// <summary>Liczba zgłoszeń „Oczekuje na wysłanie” — przed wylogowaniem ostrzegamy, że zostaną usunięte z telefonu.</summary>
    Task<int> CountPendingAsync(CancellationToken ct = default);

    /// <summary>Wyrejestrowanie urządzenia (push) → usunięcie sesji → usunięcie niewysłanych zgłoszeń i ich zdjęć. Brak sieci nie blokuje wylogowania.</summary>
    Task SignOutAsync(CancellationToken ct = default);
}
