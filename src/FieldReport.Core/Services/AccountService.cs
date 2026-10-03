using FieldReport.Core.Models;
using FieldReport.Core.Services.Push;

namespace FieldReport.Core.Services;

/// <summary>Konto technika: dane sesji i wylogowanie (wyrejestrowanie telefonu z powiadomień + usunięcie sesji).</summary>
public sealed class AccountService(IAuthService auth, IDeviceRegistrationService devices) : IAccountService
{
    public Task<Session?> GetSessionAsync(CancellationToken ct = default) => auth.GetSessionAsync(ct);

    public async Task SignOutAsync(CancellationToken ct = default)
    {
        await devices.UnregisterAsync(ct); // najpierw urządzenie (potrzebny jeszcze token logowania)
        await auth.SignOutAsync(ct);
    }
}
