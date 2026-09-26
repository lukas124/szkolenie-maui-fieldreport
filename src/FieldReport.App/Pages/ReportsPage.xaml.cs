using FieldReport.App.Extensions;
using FieldReport.Core.Services;

namespace FieldReport.App.Pages;

public partial class ReportsPage : BasePage
{
    public ReportsPage(IErrorHandler errors) : base(errors)
    {
        InitializeComponent();
        ActionCommand = new Command(OpenSettings); // zębatka w nagłówku
    }

    // Zębatka w nagłówku: otwiera Ustawienia na stosie nawigacji
    private void OpenSettings() => Navigation.PushAsync(new SettingsPage(Errors)).FireAndForget(Errors);
}
