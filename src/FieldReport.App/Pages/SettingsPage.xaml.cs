using FieldReport.App.Extensions;
using FieldReport.Core.Services;

namespace FieldReport.App.Pages;

public partial class SettingsPage : BasePage
{
    public SettingsPage(IErrorHandler errors) : base(errors)
    {
        InitializeComponent();
    }

    // Przycisk „Playground”: otwiera stronę piaskownicy na stosie nawigacji
    private void OnPlaygroundClicked(object? sender, EventArgs e) =>
        Navigation.PushAsync(new PlaygroundPage(Errors)).FireAndForget(Errors);
}
