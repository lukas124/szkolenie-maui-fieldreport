using FieldReport.Core.Models;

namespace FieldReport.Core.Services;

/// <summary>
/// Logowanie technika i sesja. Implementacja: <see cref="ApiAuthService"/> (API szkoleniowe).
/// </summary>
public interface IAuthService
{
    Task<bool> HasValidSessionAsync(CancellationToken ct = default);
    Task<Result> SignInAsync(string email, string pin, CancellationToken ct = default);
    Task<Session?> GetSessionAsync(CancellationToken ct = default);
    Task SignOutAsync(CancellationToken ct = default);

    /// <summary>
    /// Nowa para tokenów z refresh tokenu (wołane przez AuthHeaderHandler po 401). <paramref name="usedAccessToken"/>
    /// to token, który serwer odrzucił: jeśli w sesji jest już inny, ktoś właśnie odświeżył i nic nie robimy.
    /// Nieudane odświeżenie (refresh token nieważny) usuwa sesję i wysyła <see cref="SessionExpiredMessage"/> → ekran logowania.
    /// </summary>
    Task<Result> RefreshSessionAsync(string usedAccessToken, CancellationToken ct = default);
}
