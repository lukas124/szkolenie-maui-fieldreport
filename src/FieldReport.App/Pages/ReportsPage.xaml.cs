using FieldReport.App.Extensions;
using FieldReport.Core.Data;
using FieldReport.Core.Services;

namespace FieldReport.App.Pages;

public partial class ReportsPage : BasePage
{
    private readonly IReportRepository _repository; // dla narzędzi dewelopera w Playground (Ustawienia → Playground)

    public ReportsPage(IErrorHandler errors, IReportRepository repository) : base(errors)
    {
        InitializeComponent();
        _repository = repository;
        ActionCommand = new Command(OpenSettings); // zębatka w nagłówku
    }

    // Zębatka w nagłówku: otwiera Ustawienia na stosie nawigacji
    private void OpenSettings() => Navigation.PushAsync(new SettingsPage(Errors, _repository)).FireAndForget(Errors);
}
