using System.ComponentModel;
using Android.App;
using Android.Runtime;
using FieldReport.Core.Services;

namespace FieldReport.App;

// HTTP bez TLS (lokalne API z emulatora: http://10.0.2.2:5080/) tylko w Debug. W Release zostaje domyślne „tylko HTTPS”.
#if DEBUG
[Application(UsesCleartextTraffic = true)]
#else
[Application]
#endif
public class MainApplication : MauiApplication
{
    /// <summary>Kanał powiadomień zgłoszeń (Android 8+). Ten sam identyfikator wysyła API (AndroidNotification.ChannelId).</summary>
    public const string ReportsChannelId = "reports";

    public MainApplication(IntPtr handle, JniHandleOwnership ownership)
        : base(handle, ownership)
    {
    }

    public override void OnCreate()
    {
        base.OnCreate();
        var localizer = IPlatformApplication.Current!.Services.GetRequiredService<ILocalizer>();
        // przed pierwszym powiadomieniem (także gdy proces startuje z powodu push)
        CreateNotificationChannel(localizer);

        // Zmiana języka w aplikacji: tworzymy kanał ponownie z tym samym identyfikatorem — Android od razu
        // zmienia jego nazwę i opis w ustawieniach systemu
        ((INotifyPropertyChanged)localizer).PropertyChanged += (_, _) => CreateNotificationChannel(localizer);
    }

    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

    // Nazwa i opis kanału w bieżącym języku aplikacji (widać je w ustawieniach powiadomień aplikacji w systemie)
    private void CreateNotificationChannel(ILocalizer localizer)
    {
        // kanały od Androida 8
        if (!OperatingSystem.IsAndroidVersionAtLeast(26)) return;
        var channel = new NotificationChannel(ReportsChannelId, localizer.GetString("Push_ChannelName"), NotificationImportance.High)
        {
            Description = localizer.GetString("Push_ChannelDescription")
        };
        ((NotificationManager)GetSystemService(NotificationService)!).CreateNotificationChannel(channel);
    }
}
