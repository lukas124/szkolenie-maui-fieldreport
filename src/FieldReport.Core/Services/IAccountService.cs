using FieldReport.Core.Models;

namespace FieldReport.Core.Services;

/// <summary>Konto technika w Ustawieniach: kim jestem + wylogowanie jako proces (kilka kroków = usługa, nie ViewModel).</summary>
public interface IAccountService
{
    Task<Session?> GetSessionAsync(CancellationToken ct = default);

    /// <summary>Wyrejestrowanie urządzenia (push) → usunięcie sesji. Brak sieci nie blokuje wylogowania.</summary>
    Task SignOutAsync(CancellationToken ct = default);
}
