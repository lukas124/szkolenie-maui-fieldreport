using FieldReport.App.Navigation;
using FieldReport.App.Pages;
using Microsoft.Extensions.Logging;

namespace FieldReport.App;

public partial class App : Application
{
    private readonly IServiceProvider _services;
    private readonly ILogger<App> _logger;

    public App(IServiceProvider services, ILogger<App> logger)
    {
        InitializeComponent();
        _services = services;
        _logger = logger;
    }

    // Tworzy okno aplikacji i loguje zdarzenia jego cyklu życia (widać je w adb logcat -s DOTNET).
    // Okno może powstać więcej niż raz w czasie życia aplikacji: na Androidzie np. po zamknięciu aplikacji
    // przyciskiem Wstecz i ponownym otwarciu z ekranu głównego proces wciąż działa, więc konstruktor App
    // nie wykona się drugi raz, a CreateWindow — tak. Dlatego kod startowy podpinamy do zdarzeń okna.
    protected override Window CreateWindow(IActivationState? activationState)
    {
        var root = _services.GetRequiredService<RootPage>();
        var window = new Window(root);

        window.Created += async (_, _) =>
        {
            _logger.LogInformation("Window: Created");
            try
            {
                await root.InitializeAsync(); // ładowanie pierwszej strony (listy)
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Startup failed"); // async void w handlerze: zawsze try/catch
            }
        };
        window.Activated += (_, _) => _logger.LogInformation("Window: Activated");
        window.Deactivated += (_, _) => _logger.LogInformation("Window: Deactivated");
        window.Stopped += (_, _) => _logger.LogInformation("Window: Stopped (app in background)");
        window.Resumed += (_, _) => _logger.LogInformation("Window: Resumed (app back in foreground)");
        window.Destroying += (_, _) => _logger.LogInformation("Window: Destroying");

        return window;
    }
}
