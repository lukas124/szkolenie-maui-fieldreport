using FieldReport.App.Extensions;
using FieldReport.Core.Data;
using FieldReport.Core.Services;

namespace FieldReport.App.Pages;

public partial class SettingsPage : BasePage
{
    private readonly IReportRepository _repository; // przekazujemy dalej do Playground

    public SettingsPage(IErrorHandler errors, IReportRepository repository) : base(errors)
    {
        InitializeComponent();
        _repository = repository;
    }

    // Przycisk „Playground”: otwiera stronę piaskownicy na stosie nawigacji
    private void OnPlaygroundClicked(object? sender, EventArgs e) =>
        Navigation.PushAsync(new PlaygroundPage(Errors, _repository)).FireAndForget(Errors);
}
