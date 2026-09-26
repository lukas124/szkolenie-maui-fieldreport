using Plugin.Firebase.CloudMessaging;
using Plugin.Firebase.CloudMessaging.Platforms.iOS.Extensions;
using UserNotifications;

namespace FieldReport.App;

/// <summary>
/// Delegat UNUserNotificationCenter (iOS): system pyta go, co zrobić z powiadomieniem. W aplikacji może być tylko
/// JEDEN delegat, więc wszystko przekazujemy do pluginu Firebase. Jedna zmiana: gdy aplikacja jest na pierwszym planie,
/// iOS nie pokazuje systemowego banera — pokazujemy własny baner w aplikacji (jak na Androidzie).
/// Tapnięcie powiadomienia obsługuje plugin (DidReceiveNotificationResponse).
/// </summary>
public sealed class ForegroundNotificationDelegate(FirebaseCloudMessagingImplementation plugin) : UNUserNotificationCenterDelegate
{
    // co zrobić z powiadomieniem, gdy aplikacja jest na pierwszym planie?
    public override void WillPresentNotification(UNUserNotificationCenter center, UNNotification notification,
        Action<UNNotificationPresentationOptions> completionHandler)
    {
        // → FirebasePushAdapter → PushReceivedMessage
        plugin.OnNotificationReceived(notification.ToFCMNotification());
        // bez systemowego banera iOS
        completionHandler(UNNotificationPresentationOptions.None);
    }

    // tapnięcie w powiadomienie → plugin → NotificationTapped
    public override void DidReceiveNotificationResponse(UNUserNotificationCenter center, UNNotificationResponse response,
        Action completionHandler) => plugin.DidReceiveNotificationResponse(center, response, completionHandler);
}
