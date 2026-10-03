using FieldReport.App.Navigation;
using FieldReport.App.Pages;
using FieldReport.App.Services;
using FieldReport.Core.Presentation;
using FieldReport.Core.Services;
using FieldReport.Core.ViewModels;
using Microsoft.Extensions.Logging;

namespace FieldReport.App;

public partial class App : Application
{
    private readonly ILocalizer _localizer;
    private readonly ILogger<App> _logger;

    public App(ILocalizer localizer, ILogger<App> logger)
    {
        InitializeComponent();
        _localizer = localizer;
        _logger = logger;
    }

    // Tworzy okno aplikacji i loguje zdarzenia jego cyklu życia (widać je w adb logcat -s DOTNET).
    // Okno może powstać więcej niż raz w czasie życia aplikacji: na Androidzie np. po zamknięciu aplikacji
    // przyciskiem Wstecz i ponownym otwarciu z ekranu głównego proces wciąż działa, więc konstruktor App
    // nie wykona się drugi raz, a CreateWindow — tak. Dlatego kod startowy podpinamy do zdarzeń okna.
    protected override Window CreateWindow(IActivationState? activationState)
    {
        // Obiekty aplikacji tworzymy ręcznie, w jednym miejscu: usługi → ViewModel → strona.
        // ViewModel dostaje usługi w konstruktorze i nie wie, skąd pochodzą.
        var dialogs = new MauiDialogService();
        var errors = new LoggingErrorHandler(_logger);
        var navigation = new NavigationService();
        var reports = new SampleReportService();
        var items = new ReportItemFactory(/* TODO: przekaż new ReportFormatter(_localizer) */);
        var feedback = new UserFeedback(dialogs, new ErrorMessageProvider(_localizer), _localizer);
        var viewModel = new ReportsViewModel(navigation /* TODO: przekaż też reports, items, feedback */);
        var page = new ReportsPage(errors) { BindingContext = viewModel };

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
