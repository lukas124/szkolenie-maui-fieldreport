using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Content.Res;
using Android.OS;
using AndroidX.Core.View;
using Plugin.Firebase.CloudMessaging;

namespace FieldReport.App;

[Activity(
    Theme = "@style/Maui.SplashTheme",
    MainLauncher = true,
    LaunchMode = LaunchMode.SingleTop,
    // Zmiany konfiguracji, które aktywność obsługuje sama (obrót, motyw, rozmiar czcionki, język, klawiatura…).
    // Bez nich Android zamyka i tworzy aktywność od nowa, a razem z nią okno MAUI i cały stos stron.
    ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode |
                           ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density |
                           ConfigChanges.FontScale | ConfigChanges.Locale | ConfigChanges.LayoutDirection |
                           ConfigChanges.Keyboard | ConfigChanges.KeyboardHidden | ConfigChanges.Navigation)]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        UpdateSystemBars(Resources?.Configuration); // ikony pasków systemowych

        // Powiadomienia push: kanał i pierwszy plan — na pierwszym planie
        // nie pokazujemy powiadomienia systemowego, tylko baner w aplikacji
        // (FirebasePushAdapter → PushReceivedMessage)
        FirebaseCloudMessagingImplementation.ChannelId = MainApplication.ReportsChannelId;
        FirebaseCloudMessagingImplementation.ShowLocalNotificationAction = _ => { };
        // tapnięcie, które uruchomiło aplikację (zimny start)
        FirebaseCloudMessagingImplementation.OnNewIntent(Intent!);
    }

    protected override void OnNewIntent(Intent? intent)
    {
        base.OnNewIntent(intent);
        // tapnięcie, gdy aplikacja była w tle
        if (intent is not null) FirebaseCloudMessagingImplementation.OnNewIntent(intent);
    }

    public override void OnConfigurationChanged(Configuration newConfig)
    {
        base.OnConfigurationChanged(newConfig);
        UpdateSystemBars(newConfig); // zmiana trybu jasny/ciemny bez odtwarzania aktywności (ConfigChanges.UiMode)
    }

    // Pod paskami systemowymi (status, nawigacja) widać tło strony, więc ich ikony muszą pasować do motywu.
    // Przy zmianie motywu w trakcie działania aktywność nie jest tworzona od nowa (ConfigChanges.UiMode), więc przełączamy je tutaj.
    private void UpdateSystemBars(Configuration? configuration)
    {
        if (Window is null) return;
        var isNight = (configuration?.UiMode & UiMode.NightMask) == UiMode.NightYes;
        SetBarIcons(Window, isNight);

        // Strona modalna MAUI (np. kreator) ma własne okno dialogu — jej pasek statusu też trzeba przełączyć
        foreach (var dialog in SupportFragmentManager.Fragments.OfType<AndroidX.Fragment.App.DialogFragment>())
            if (dialog.Dialog?.Window is { } modalWindow) SetBarIcons(modalWindow, isNight);
    }

    // Ikony paska statusu i nawigacji: ciemne na jasnym tle, jasne na ciemnym
    private static void SetBarIcons(Android.Views.Window window, bool isNight)
    {
        var controller = WindowCompat.GetInsetsController(window, window.DecorView);
        if (controller is null) return;
        controller.AppearanceLightStatusBars = !isNight;
        controller.AppearanceLightNavigationBars = !isNight;
    }
}
