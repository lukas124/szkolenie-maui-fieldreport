using FieldReport.App.Extensions;
using FieldReport.Core.Services;
using FieldReport.Core.ViewModels;

namespace FieldReport.App.Pages;

public partial class PlaygroundPage : BasePage
{
    public PlaygroundPage(IErrorHandler errors) : base(errors)
    {
        InitializeComponent();
    }

    // Narzędzia dewelopera: lista zgłoszeń to pierwsza strona na stosie nawigacji, sięgamy do jej ViewModelu
    private ReportsViewModel ReportsList => (ReportsViewModel)Navigation.NavigationStack[0].BindingContext;

    // „Wyczyść listę”: opróżnia listę na ekranie, żeby zobaczyć pusty stan (dane przykładowe zostają bez zmian)
    private void OnClearReportsClicked(object? sender, EventArgs e)
    {
        ReportsList.Reports.Clear();
        SampleDataStatus.Text = "Lista wyczyszczona – wróć na listę.";
    }

    // „Przywróć przykłady”: wczytuje listę od nowa z usługi (6 zgłoszeń przykładowych)
    private void OnRestoreSamplesClicked(object? sender, EventArgs e)
    {
        ReportsList.OnNavigatedToAsync(null).FireAndForget(Errors);
        SampleDataStatus.Text = "Przywrócono przykłady – wróć na listę.";
    }
}
