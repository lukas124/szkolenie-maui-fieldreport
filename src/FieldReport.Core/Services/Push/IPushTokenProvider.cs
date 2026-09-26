namespace FieldReport.Core.Services.Push;

/// <summary>Token rejestracyjny FCM. Implementacja w App (FirebasePushAdapter). Brak tokenu (np. iOS bez APNs) = null, nie wyjątek.</summary>
public interface IPushTokenProvider
{
    event EventHandler<string>? TokenChanged;
    Task<string?> GetTokenAsync(CancellationToken ct = default);

    /// <summary>Usuwa token lokalnie w SDK Firebase (wylogowanie): stary token przestaje być ważny, kolejny GetToken da nowy.</summary>
    Task DeleteTokenAsync(CancellationToken ct = default);
}
