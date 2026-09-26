using FieldReport.App.Navigation;
using FieldReport.App.Pages;
using FieldReport.Core.Navigation;
using FieldReport.Core.ViewModels;
using Microsoft.Extensions.Logging;

namespace FieldReport.App;

public partial class App : Application
{
    private readonly IServiceProvider _services;
    private readonly INavigationService _navigation;
    private readonly ILogger<App> _logger;

    public App(IServiceProvider services, INavigationService navigation, ILogger<App> logger)
    {
        InitializeComponent();
        _services = services;
        _navigation = navigation;
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
                await root.InitializeAsync(); // start aplikacji: decyzja startowa — logowanie albo lista (StartupViewModel)
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Startup failed"); // async void w handlerze: zawsze try/catch
                try
                {
                    await _navigation.ResetToAsync<StartupErrorViewModel>();
                }
                catch (Exception fatal)
                {
                    _logger.LogCritical(fatal, "Startup error page failed");
                }
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
