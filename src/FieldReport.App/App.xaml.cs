using FieldReport.App.Navigation;
using FieldReport.App.Pages;
using FieldReport.Core.ViewModels;
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
        // App to „composition root”: jedyne miejsce, w którym prosimy kontener DI o gotowe obiekty.
        // Kontener sam tworzy ViewModel razem z jego zależnościami (rejestracje: ServiceCollectionExtensions).
        var page = _services.GetRequiredService<ReportsPage>();
        var viewModel = _services.GetRequiredService<ReportsViewModel>();
        page.BindingContext = viewModel;

        var window = new Window(new NavigationPage(page));

        window.Created += async (_, _) =>
        {
            _logger.LogInformation("Window: Created");
            try
            {
                viewModel.IsBusy = true;
                await viewModel.OnNavigatedToAsync(null); // jednorazowe ładowanie listy
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Startup failed"); // async void w handlerze: zawsze try/catch
            }
            finally
            {
                viewModel.IsBusy = false;
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
