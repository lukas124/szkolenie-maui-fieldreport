using CommunityToolkit.Maui;
using FieldReport.App.Navigation;
using FieldReport.App.Pages;
using FieldReport.Core.ViewModels;
using FieldReport.Core.ViewModels.NewReport;
using FieldReport.Core.ViewModels.Playground;
using FieldReport.App.Services;
using FieldReport.Core.Data.Api;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.LifecycleEvents;

namespace FieldReport.App;

// Odpowiednik Program.cs: konfiguracja aplikacji (fonty, logowanie, wygląd kontrolek, kontener DI).
public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit() // gotowe konwertery, Snackbar (baner push na pierwszym planie)
            .RegisterFirebaseServices()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("IBMPlexSans-Regular.ttf", "PlexRegular");
                fonts.AddFont("IBMPlexSans-SemiBold.ttf", "PlexSemiBold");
                fonts.AddFont("IBMPlexSans-Bold.ttf", "PlexBold");
                fonts.AddFont("JetBrainsMono-Regular.ttf", "MonoRegular");
                fonts.AddFont("JetBrainsMono-Bold.ttf", "MonoBold");
            });

        ConfigureHandlers(); // mapper Entry/Editor (Android)
        builder.AddAppConfiguration(); // appsettings.json + appsettings.{Configuration}.json

        // Kontener DI: rejestracje pogrupowane w metodach rozszerzających (ServiceCollectionExtensions)
        builder.Services
            .AddCoreServices()
            .AddPlatformServices()
            .AddApiClients()
            .AddViewModels()
            .AddPages();
        builder.Services.AddSingleton(CreatePageRoutes());

#if DEBUG
        builder.Logging.AddDebug(); // okno Output w IDE (z debuggerem)
        builder.Logging.AddSimpleConsole(o => o.SingleLine = true); // Android: adb logcat -s DOTNET
        builder.Logging.SetMinimumLevel(LogLevel.Debug);
#endif

        return builder.Build();
    }

    // Mapa nawigacji: który ViewModel wyświetla która strona
    // (używa jej NavigationService)
    private static PageRoutes CreatePageRoutes()
    {
        // ViewModel → strona
        var routes = new PageRoutes();
        routes.Map<StartupViewModel, StartupPage>();
        routes.Map<StartupErrorViewModel, StartupErrorPage>();
        routes.Map<LoginViewModel, LoginPage>();
        // lista zgłoszeń
        routes.Map<ReportsViewModel, ReportsPage>();
        // szczegóły zgłoszenia
        routes.Map<ReportDetailsViewModel, ReportDetailsPage>();
        // kreator nowego zgłoszenia
        routes.Map<NewReportFlowViewModel, NewReportFlowPage>();
        // ustawienia (zębatka)
        routes.Map<SettingsViewModel, SettingsPage>();
        routes.Map<NotificationPrimingViewModel, NotificationPrimingPage>();
        // piaskownica
        routes.Map<PlaygroundViewModel, PlaygroundPage>();
        return routes;
    }

    // Konfiguracja per środowisko: zasoby osadzone (csproj) — do binarki trafia tylko plik bieżącej konfiguracji
    public static MauiAppBuilder AddAppConfiguration(this MauiAppBuilder builder)
    {
        var assembly = typeof(MauiProgram).Assembly;
        using var baseStream = assembly.GetManifestResourceStream("appsettings.json")
            ?? throw new InvalidOperationException("appsettings.json is missing.");
        using var envStream = assembly.GetManifestResourceStream("appsettings.environment.json");

        var config = new ConfigurationBuilder().AddJsonStream(baseStream);
        if (envStream is not null) config.AddJsonStream(envStream); // nadpisuje bazę

        builder.Configuration.AddConfiguration(config.Build());
        builder.Services.Configure<ApiSettings>(builder.Configuration.GetSection("Api"));
        builder.Services.Configure<AppEnvironmentSettings>(builder.Configuration.GetSection("Environment"));
        return builder;
    }

    // powiadomienia push: inicjalizacja Firebase w cyklu życia platformy (Plugin.Firebase)
    private static MauiAppBuilder RegisterFirebaseServices(this MauiAppBuilder builder)
    {
        builder.ConfigureLifecycleEvents(events =>
        {
#if IOS
            events.AddiOS(ios => ios.WillFinishLaunching((_, _) =>
            {
                Plugin.Firebase.Core.Platforms.iOS.CrossFirebase.Initialize();
                // ustawia delegata UNUserNotificationCenter (tapnięcia, pierwszy plan) i prosi o token APNs —
                // na symulatorze / bez capability Push token nie powstanie, ale aplikacja działa dalej
                Plugin.Firebase.CloudMessaging.FirebaseCloudMessagingImplementation.Initialize();
                // pierwszy plan: baner w aplikacji zamiast systemowego (tapnięcia dalej obsługuje plugin)
                UserNotifications.UNUserNotificationCenter.Current.Delegate = new ForegroundNotificationDelegate(
                    (Plugin.Firebase.CloudMessaging.FirebaseCloudMessagingImplementation)
                    Plugin.Firebase.CloudMessaging.CrossFirebaseCloudMessaging.Current);
                return false;
            }));
#elif ANDROID
            events.AddAndroid(android => android.OnCreate((activity, _) =>
                Plugin.Firebase.Core.Platforms.Android.CrossFirebase.Initialize(activity, () => Platform.CurrentActivity!)));
#endif
        });
        return builder;
    }

    // Dostosowanie natywnych kontrolek przez mapper handlera (bez własnych rendererów).
    private static void ConfigureHandlers()
    {
#if ANDROID
        // Entry/Editor leżą w naszej ramce (styl InputFrame), więc chowamy natywne podkreślenie Androida.
        Microsoft.Maui.Handlers.EntryHandler.Mapper.AppendToMapping("NoUnderline", (handler, _) =>
            handler.PlatformView.BackgroundTintList =
                Android.Content.Res.ColorStateList.ValueOf(Android.Graphics.Color.Transparent));
        Microsoft.Maui.Handlers.PickerHandler.Mapper.AppendToMapping("NoUnderline", (handler, _) =>
            handler.PlatformView.BackgroundTintList =
                Android.Content.Res.ColorStateList.ValueOf(Android.Graphics.Color.Transparent));
        Microsoft.Maui.Handlers.EditorHandler.Mapper.AppendToMapping("NoUnderline", (handler, _) =>
            handler.PlatformView.BackgroundTintList =
                Android.Content.Res.ColorStateList.ValueOf(Android.Graphics.Color.Transparent));
#elif IOS
        // iOS: bez natywnej ramki UITextField (ramkę daje styl InputFrame); Picker też jest polem UITextField
        Microsoft.Maui.Handlers.EntryHandler.Mapper.AppendToMapping("NoBorder", (handler, _) =>
            handler.PlatformView.BorderStyle = UIKit.UITextBorderStyle.None);
        Microsoft.Maui.Handlers.PickerHandler.Mapper.AppendToMapping("NoBorder", (handler, _) =>
            handler.PlatformView.BorderStyle = UIKit.UITextBorderStyle.None);
#endif
    }
}
