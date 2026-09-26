using Android.App;
using Android.Content.PM;
using Android.Content.Res;
using Android.OS;
using AndroidX.Core.View;

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
