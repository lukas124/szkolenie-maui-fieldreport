using Microsoft.Extensions.Logging;

namespace FieldReport.App;

// Odpowiednik Program.cs: konfiguracja aplikacji (fonty, logowanie, wygląd kontrolek).
public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("IBMPlexSans-Regular.ttf", "PlexRegular");
                fonts.AddFont("IBMPlexSans-SemiBold.ttf", "PlexSemiBold");
                fonts.AddFont("IBMPlexSans-Bold.ttf", "PlexBold");
                fonts.AddFont("JetBrainsMono-Regular.ttf", "MonoRegular");
                fonts.AddFont("JetBrainsMono-Bold.ttf", "MonoBold");
            });

        ConfigureHandlers(); // mapper Entry/Editor (Android)

#if DEBUG
        builder.Logging.AddDebug(); // okno Output w IDE (z debuggerem)
        builder.Logging.AddSimpleConsole(o => o.SingleLine = true); // Android: adb logcat -s DOTNET
        builder.Logging.SetMinimumLevel(LogLevel.Debug);
#endif

        return builder.Build();
    }

    // Dostosowanie natywnych kontrolek przez mapper handlera (bez własnych rendererów).
    private static void ConfigureHandlers()
    {
#if ANDROID
        // Entry/Editor leżą w naszej ramce (styl InputFrame), więc chowamy natywne podkreślenie Androida.
        Microsoft.Maui.Handlers.EntryHandler.Mapper.AppendToMapping("NoUnderline", (handler, _) =>
            handler.PlatformView.BackgroundTintList =
                Android.Content.Res.ColorStateList.ValueOf(Android.Graphics.Color.Transparent));
        Microsoft.Maui.Handlers.EditorHandler.Mapper.AppendToMapping("NoUnderline", (handler, _) =>
            handler.PlatformView.BackgroundTintList =
                Android.Content.Res.ColorStateList.ValueOf(Android.Graphics.Color.Transparent));
#endif
    }
}
