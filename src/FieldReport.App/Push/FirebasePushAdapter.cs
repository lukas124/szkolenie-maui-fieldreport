using CommunityToolkit.Mvvm.Messaging;
using FieldReport.App.Extensions;
using FieldReport.Core.Navigation;
using FieldReport.Core.Services;
using FieldReport.Core.Services.Push;
using Microsoft.Extensions.Logging;
using Plugin.Firebase.CloudMessaging;

namespace FieldReport.App.Push;

/// <summary>
/// Adapter: jedyna klasa w aplikacji, która zna plugin Firebase (Plugin.Firebase.CloudMessaging 4.x).
/// Tłumaczy zdarzenia pluginu na nasze typy, więc reszta kodu (i testy) nie zależy od Firebase:
/// token FCM → IPushTokenProvider, tapnięcie powiadomienia → IDeepLinkRouter, powiadomienie na pierwszym planie → IMessenger.
/// </summary>
// TODO: dodaj parametry konstruktora IPushMessageRouter router i IDeepLinkRouter deepLinks (po fcm)
public sealed class FirebasePushAdapter(
    IFirebaseCloudMessaging fcm, IMessenger messenger, IErrorHandler errors, ILogger<FirebasePushAdapter> logger) : IPushTokenProvider
{
    public event EventHandler<string>? TokenChanged;

    // Podpina zdarzenia pluginu (wołane raz, z PushStartup przy starcie aplikacji)
    public void Start()
    {
        // Firebase wydał nowy token (pierwsze uruchomienie, reinstalacja, odświeżenie) → informujemy PushStartup
        fcm.TokenChanged += (_, e) =>
        {
            if (!string.IsNullOrEmpty(e.Token)) TokenChanged?.Invoke(this, e.Token);
        };
        // Tapnięcie powiadomienia, gdy aplikacja była w tle albo zamknięta → otwieramy szczegóły zgłoszenia
        fcm.NotificationTapped += (_, e) => OnTappedAsync(e.Notification.Data).FireAndForget(errors);
        // Powiadomienie przyszło, gdy aplikacja jest na ekranie → komunikat dla ForegroundPushHandler (baner w aplikacji)
        fcm.NotificationReceived += (_, e) =>
            MainThread.BeginInvokeOnMainThread(() =>
                messenger.Send(new PushReceivedMessage(ToReadOnly(e.Notification.Data), e.Notification.Body)));
        fcm.Error += (_, e) => logger.LogWarning("FCM: {Message}", e.Message);
    }

    // Uwaga: CheckIfValidAsync() pluginu na Androidzie sam wywołuje TokenChanged (odświeżenie tokenu),
    // a na iOS prosi o zgodę na powiadomienia — dlatego nie wołamy go przy każdym pobraniu tokenu.
    public async Task<string?> GetTokenAsync(CancellationToken ct = default)
    {
        try
        {
            var token = await fcm.GetTokenAsync();
            return string.IsNullOrEmpty(token) ? null : token;
        }
        catch (Exception ex) // iOS bez APNs, emulator bez Google Play: brak tokenu to nie błąd aplikacji
        {
            logger.LogInformation("FCM token unavailable: {Message}", ex.Message);
            return null;
        }
    }

    // Wylogowanie: token przestaje być ważny w FCM (push do starego tokenu → „Unregistered”, API go usuwa).
    // Plugin 4.0.1 nie ma tej metody w IFirebaseCloudMessaging — wołamy natywne SDK. Offline: best effort.
    public async Task DeleteTokenAsync(CancellationToken ct = default)
    {
        try
        {
#if ANDROID
            await Android.Gms.Extensions.TasksExtensions.AsAsync(Firebase.Messaging.FirebaseMessaging.Instance.DeleteToken());
#elif IOS
            await Firebase.CloudMessaging.Messaging.SharedInstance.DeleteTokenAsync();
#else
            await Task.CompletedTask;
#endif
        }
        catch (Exception ex)
        {
            logger.LogInformation("FCM token not deleted: {Message}", ex.Message);
        }
    }

    // TODO: dane powiadomienia → router.ToDeepLink(ToReadOnly(data)); jest link → deepLinks.RouteAsync(link),
    // nieznany typ (null) → nic nie robimy
    private Task OnTappedAsync(IDictionary<string, string>? data) => Task.CompletedTask;

    private static IReadOnlyDictionary<string, string> ToReadOnly(IDictionary<string, string>? data) =>
        data is null ? new Dictionary<string, string>() : new Dictionary<string, string>(data);
}
