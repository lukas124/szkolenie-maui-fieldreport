using CommunityToolkit.Mvvm.Messaging;
using FieldReport.Core.Data.Api;
using FieldReport.Core.Models;

namespace FieldReport.Core.Services;

/// <summary>
/// Logowanie przez API szkoleniowe. Tokeny → ISessionStore (SecureStorage).
/// 401 przy logowaniu = zły e-mail lub PIN; brak sieci → Errors.Network.
/// </summary>
public sealed class ApiAuthService(IAuthApiClient api, ISessionStore sessions, IMessenger messenger) : IAuthService
{
    // jedno odświeżenie naraz dla całej aplikacji (kilka równoległych 401 → jedno POST api/auth/refresh)
    private static readonly SemaphoreSlim RefreshLock = new(1, 1);

    public async Task<bool> HasValidSessionAsync(CancellationToken ct = default) =>
        await sessions.GetAsync() is not null; // wygasły access token odświeży AuthHeaderHandler

    public async Task<Result> SignInAsync(string email, string pin, CancellationToken ct = default)
    {
        var address = email.Trim();
        var result = await api.LoginAsync(address, pin, ct);
        if (!result.IsSuccess)
            return Result.Failure(result.Error == Errors.Unauthorized ? Errors.InvalidCredentials : result.Error!);

        await sessions.SaveAsync(ToSession(address, result.Value!));
        return Result.Success();
    }

    public Task<Session?> GetSessionAsync(CancellationToken ct = default) => sessions.GetAsync();

    public Task SignOutAsync(CancellationToken ct = default) => sessions.ClearAsync();

    public async Task<Result> RefreshSessionAsync(string usedAccessToken, CancellationToken ct = default)
    {
        await RefreshLock.WaitAsync(ct);
        try
        {
            var session = await sessions.GetAsync();
            if (session is null) return Result.Failure(Errors.SessionExpired);
            // Kilka żądań naraz może dostać 401. Refresh token jest jednorazowy, więc odświeża tylko pierwsze z nich;
            // pozostałe zastają w sesji nowy token i po prostu ponawiają żądanie.
            if (session.AccessToken != usedAccessToken) return Result.Success();

            var result = await api.RefreshAsync(session.RefreshToken, ct);
            if (result.IsSuccess)
            {
                await sessions.SaveAsync(ToSession(session.Email, result.Value!));
                return Result.Success();
            }

            if (result.Error == Errors.Unauthorized) // refresh token nieważny: koniec sesji → logowanie
            {
                await sessions.ClearAsync();
                messenger.Send(new SessionExpiredMessage());
                return Result.Failure(Errors.SessionExpired);
            }

            return Result.Failure(result.Error!); // np. brak sieci: sesja zostaje, spróbujemy później
        }
        finally
        {
            RefreshLock.Release();
        }
    }

    private Session ToSession(string email, TokenResponseDto tokens) => new(email, tokens.TechnicianName, tokens.AccessToken, tokens.RefreshToken);
}
