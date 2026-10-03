using FieldReport.Core.Models;

namespace FieldReport.Core.Services;

/// <summary>
/// Logowanie technika i sesja. Implementacja <see cref="FakeAuthService"/> działa bez sieci (PIN 1234);
/// wersję z API podstawia się tylko w rejestracji DI.
/// </summary>
public interface IAuthService
{
    Task<bool> HasValidSessionAsync(CancellationToken ct = default);
    Task<Result> SignInAsync(string email, string pin, CancellationToken ct = default);
    Task<Session?> GetSessionAsync(CancellationToken ct = default);
    Task SignOutAsync(CancellationToken ct = default);
}
