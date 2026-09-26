using FieldReport.Core.Services.Push;

namespace FieldReport.App.Push;

/// <summary>
/// Zgoda na powiadomienia. Android 13+ (API 33): uprawnienie runtime POST_NOTIFICATIONS; starszy Android — włączone
/// domyślnie (użytkownik może je wyłączyć tylko w ustawieniach). iOS: UNUserNotificationCenter (pyta tylko raz).
/// </summary>
public sealed class MauiPushPermissionService : IPushPermissionService
{
    private const string AskedKey = "notifications.permission_asked";

    public Task<PushPermissionStatus> CheckAsync() => MainThread.InvokeOnMainThreadAsync(CheckOnMainThreadAsync);

    public Task<PushPermissionStatus> RequestAsync() => MainThread.InvokeOnMainThreadAsync(async () =>
    {
#if ANDROID
        if (!OperatingSystem.IsAndroidVersionAtLeast(33)) return await CheckOnMainThreadAsync();
        Preferences.Default.Set(AskedKey, true);
        await Permissions.RequestAsync<Permissions.PostNotifications>();
        return await CheckOnMainThreadAsync();
#elif IOS
        var (granted, _) = await UserNotifications.UNUserNotificationCenter.Current.RequestAuthorizationAsync(
            UserNotifications.UNAuthorizationOptions.Alert | UserNotifications.UNAuthorizationOptions.Sound |
            UserNotifications.UNAuthorizationOptions.Badge);
        return granted ? PushPermissionStatus.Granted : PushPermissionStatus.DeniedPermanently;
#else
        return PushPermissionStatus.DeniedPermanently;
#endif
    });

    private static async Task<PushPermissionStatus> CheckOnMainThreadAsync()
    {
#if ANDROID
        var enabled = AndroidX.Core.App.NotificationManagerCompat.From(Android.App.Application.Context)!.AreNotificationsEnabled();
        if (enabled) return PushPermissionStatus.Granted;
        if (!OperatingSystem.IsAndroidVersionAtLeast(33)) return PushPermissionStatus.DeniedPermanently; // tylko ustawienia

        // odmowa po pytaniu i bez „uzasadnienia” = system już nie zapyta (odmowa na stałe)
        var asked = Preferences.Default.Get(AskedKey, false);
        return asked && !Permissions.ShouldShowRationale<Permissions.PostNotifications>()
            ? PushPermissionStatus.DeniedPermanently
            : PushPermissionStatus.NotDetermined;
#elif IOS
        var settings = await UserNotifications.UNUserNotificationCenter.Current.GetNotificationSettingsAsync();
        return settings.AuthorizationStatus switch
        {
            UserNotifications.UNAuthorizationStatus.Authorized or UserNotifications.UNAuthorizationStatus.Provisional
                or UserNotifications.UNAuthorizationStatus.Ephemeral => PushPermissionStatus.Granted,
            UserNotifications.UNAuthorizationStatus.Denied => PushPermissionStatus.DeniedPermanently,
            _ => PushPermissionStatus.NotDetermined
        };
#else
        await Task.CompletedTask;
        return PushPermissionStatus.DeniedPermanently;
#endif
    }
}
