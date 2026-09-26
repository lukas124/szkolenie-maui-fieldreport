using FieldReport.Core.Models;

namespace FieldReport.Core.Services.Push;

/// <summary>Rejestracja/wyrejestrowanie tokenu FCM w API (powiadomienia push): po zgodzie, po każdej zmianie tokenu, przy wylogowaniu.</summary>
public interface IDeviceRegistrationService
{
    /// <summary>Wysyła token FCM tego telefonu do API.</summary>
    Task<Result> RegisterAsync(CancellationToken ct = default);

    /// <summary>Rejestruje tylko, gdy technik jest zalogowany i zgodził się na powiadomienia.</summary>
    Task<Result> RegisterIfPermittedAsync(CancellationToken ct = default);

    /// <summary>
    /// Start aplikacji i powrót z tła: zalogowany technik → rejestracja (jeśli jest zgoda); nikt nie jest zalogowany →
    /// usunięcie tokenu FCM (np. wylogowanie bez internetu nie zdążyło go usunąć, a telefon dalej dostawałby powiadomienia).
    /// </summary>
    Task RefreshAsync(CancellationToken ct = default);

    /// <summary>Wylogowanie: wyrejestrowanie w API (jeśli się da) i usunięcie tokenu FCM na telefonie.</summary>
    Task UnregisterAsync(CancellationToken ct = default);
}
