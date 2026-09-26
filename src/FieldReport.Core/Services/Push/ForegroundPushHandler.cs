using CommunityToolkit.Mvvm.Messaging;
using FieldReport.Core.Navigation;

namespace FieldReport.Core.Services.Push;

/// <summary>
/// Powiadomienie push na pierwszym planie: nie pokazuje się w zasobniku, tylko jako baner w aplikacji + odświeżenie listy.
/// Tapnięcie „Pokaż” na banerze = ten sam DeepLink co tapnięcie powiadomienia systemowego.
/// </summary>
public sealed class ForegroundPushHandler(
    IPushMessageRouter router, IDeepLinkRouter deepLinks, IInAppNotifier notifier,
    IMessenger messenger, ILocalizer localizer, ISessionStore sessions) : IRecipient<PushReceivedMessage>
{
    // Zaczyna nasłuch PushReceivedMessage (wołane raz, z PushStartup)
    public void Start() => messenger.Register(this);

    public void Receive(PushReceivedMessage message) => _ = ShowSafelyAsync(message);

    private async Task ShowSafelyAsync(PushReceivedMessage message)
    {
        try { await ShowAsync(message); }
        catch (Exception) { /* ignorujemy: baner jest dodatkiem, jego błąd nie może przerwać obsługi powiadomienia */ }
    }

    private async Task ShowAsync(PushReceivedMessage message)
    {
        var link = router.ToDeepLink(message.Data);
        if (link is null) return; // nieznany typ: ignorujemy
        if (await sessions.GetAsync() is null) return; // nikt nie jest zalogowany: nie pokazujemy treści zgłoszeń

        messenger.Send(new ReportsChangedMessage()); // odświeżenie listy
        await notifier.ShowAsync(BannerText(message), localizer.GetString("Push_Show"), () => deepLinks.RouteAsync(link));
    }

    private string BannerText(PushReceivedMessage message)
    {
        var type = message.Data.GetValueOrDefault("type") ?? string.Empty;
        var title = string.IsNullOrWhiteSpace(message.Body) ? localizer.GetString("Push_UnknownTitle") : message.Body;
        return string.Format(localizer.CurrentCulture, localizer.GetString($"Push_{type}"), title);
    }
}
