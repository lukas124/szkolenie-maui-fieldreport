using FieldReport.App.Extensions;
using FieldReport.Core.Services;
using FieldReport.Core.Services.Push;

namespace FieldReport.App.Push;

/// <summary>
/// Uruchamia obsługę powiadomień przy starcie aplikacji (wołane z konstruktora App):
/// podpina zdarzenia pluginu Firebase i pilnuje, żeby token FCM tego telefonu był zarejestrowany w API.
/// Usługa rejestracji jest transient (używa HttpClient z fabryki), dlatego tworzymy ją przy każdym użyciu.
/// </summary>
public sealed class PushStartup(
    FirebasePushAdapter adapter, ForegroundPushHandler foreground, Func<IDeviceRegistrationService> registration,
    IErrorHandler errors)
{
    public void Start()   // raz, z konstruktora App
    {
        // baner w aplikacji, gdy powiadomienie przyjdzie na pierwszym planie
        foreground.Start();

        // Firebase może w każdej chwili wydać nowy token → rejestrujemy go w API od razu
        adapter.TokenChanged += (_, _) => registration().RegisterIfPermittedAsync().FireAndForget(errors);
        adapter.Start();

        // Start aplikacji: zalogowany → API zna aktualny token; wylogowany → token FCM usunięty
        registration().RefreshAsync().FireAndForget(errors);
    }

    /// <summary>
    /// Powrót z tła: zgodę mogła zostać właśnie włączona w ustawieniach systemu → rejestracja;
    /// a jeśli nikt nie jest zalogowany (np. wylogowanie offline) → usunięcie tokenu FCM.
    /// </summary>
    public void OnResumed() => registration().RefreshAsync().FireAndForget(errors);
}
