namespace FieldReport.Core.Services.Push;

/// <summary>Zgoda na wyświetlanie powiadomień (Android 13+: POST_NOTIFICATIONS, iOS: UNUserNotificationCenter).</summary>
public interface IPushPermissionService
{
    Task<PushPermissionStatus> CheckAsync();
    Task<PushPermissionStatus> RequestAsync();
}
