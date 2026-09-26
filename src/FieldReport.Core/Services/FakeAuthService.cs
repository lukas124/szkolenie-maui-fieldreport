using FieldReport.Core.Models;

namespace FieldReport.Core.Services;

/// <summary>
/// Logowanie „na niby”: dowolny poprawny e-mail + PIN <c>1234</c>, bez sieci.
/// Zapisuje sesję tak samo jak logowanie przez API, więc start aplikacji i wylogowanie działają bez serwera.
/// </summary>
public sealed class FakeAuthService(ISessionStore sessions) : IAuthService
{
    public const string TrainingPin = "1234";

    public async Task<bool> HasValidSessionAsync(CancellationToken ct = default) =>
        await sessions.GetAsync() is not null;

    public async Task<Result> SignInAsync(string email, string pin, CancellationToken ct = default)
    {
        await Task.Delay(400, ct); // udajemy sieć: widać wskaźnik i blokadę przycisku
        if (pin != TrainingPin) return Result.Failure(Errors.InvalidCredentials);

        var address = email.Trim();
        await sessions.SaveAsync(new Session(address, TechnicianNames.FromEmail(address),
            AccessToken: $"fake-{Guid.NewGuid():N}", RefreshToken: $"fake-{Guid.NewGuid():N}"));
        return Result.Success();
    }

    public Task<Session?> GetSessionAsync(CancellationToken ct = default) => sessions.GetAsync();

    public Task SignOutAsync(CancellationToken ct = default) => sessions.ClearAsync();
}
