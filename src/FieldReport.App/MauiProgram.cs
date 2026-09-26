using CommunityToolkit.Maui;
using FieldReport.App.Navigation;
using FieldReport.App.Pages;
using FieldReport.Core.ViewModels;
using FieldReport.Core.ViewModels.NewReport;
using FieldReport.Core.ViewModels.Playground;
using Microsoft.Extensions.Logging;

namespace FieldReport.App;

// Odpowiednik Program.cs: konfiguracja aplikacji (fonty, logowanie, wygląd kontrolek, kontener DI).
public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit() // gotowe konwertery (np. IsNotNullConverter) i inne dodatki
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("IBMPlexSans-Regular.ttf", "PlexRegular");
                fonts.AddFont("IBMPlexSans-SemiBold.ttf", "PlexSemiBold");
                fonts.AddFont("IBMPlexSans-Bold.ttf", "PlexBold");
                fonts.AddFont("JetBrainsMono-Regular.ttf", "MonoRegular");
                fonts.AddFont("JetBrainsMono-Bold.ttf", "MonoBold");
            });

        ConfigureHandlers(); // mapper Entry/Editor (Android)

        // Kontener DI: rejestracje pogrupowane w metodach rozszerzających (ServiceCollectionExtensions)
        builder.Services
            .AddCoreServices()
            .AddPlatformServices()
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
        // piaskownica
        routes.Map<PlaygroundViewModel, PlaygroundPage>();
        return routes;
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
