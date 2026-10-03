using FieldReport.Core.Data;
using FieldReport.Core.Services;
using FieldReport.Core.ViewModels;

namespace FieldReport.App.Pages;

public partial class PlaygroundPage : BasePage
{
    private readonly IReportRepository _repository; // narzędzia dewelopera: dane przykładowe

    public PlaygroundPage(IErrorHandler errors, IReportRepository repository) : base(errors)
    {
        InitializeComponent();
        _repository = repository;
    }

    // Źle — blokuje wątek UI: aplikacja „zamarza”
    // (przy dłuższym czasie Android pokaże ANR)
    private void OnBlockingClicked(object? sender, EventArgs e)
    {
        // ten tekst nawet się nie narysuje — wątek UI śpi
        UiThreadStatus.Text = "Zamrożone…";
        Thread.Sleep(3000);
        UiThreadStatus.Text = "Thread.Sleep: UI było zablokowane przez 3 s.";
    }

    // Dobrze — await oddaje wątek UI: animacje i przewijanie działają
    private async void OnAwaitClicked(object? sender, EventArgs e)
    {
        try
        {
            UiThreadStatus.Text = "Czekam… (UI działa, możesz przewijać)";
            await Task.Delay(3000);
            UiThreadStatus.Text = "await: UI przez cały czas reagowało.";
        }
        catch (Exception ex)
        {
            // async void tylko w handlerach i zawsze z try/catch
            System.Diagnostics.Debug.WriteLine(ex);
        }
    }

    // Narzędzia dewelopera. Lista zgłoszeń to pierwsza strona na stosie nawigacji. Jej ViewModel wczytuje dane raz,
    // więc po zmianie danych wczytujemy listę ponownie.
    private ReportsViewModel ReportsList => (ReportsViewModel)Navigation.NavigationStack[0].BindingContext;

    // „Wyczyść listę”: usuwa z repozytorium wszystkie zgłoszenia, także wysłane (narzędzie, nie akcja użytkownika)
    private async void OnClearReportsClicked(object? sender, EventArgs e)
    {
        try
        {
            await SampleReports.RemoveAllAsync(_repository);
            await ReportsList.OnNavigatedToAsync(null);
            SampleDataStatus.Text = "Lista wyczyszczona – wróć na listę.";
        }
        catch (Exception ex)
        {
            Errors.Handle(ex); // async void tylko w handlerach i zawsze z try/catch
        }
    }

    // „Przywróć przykłady”: wraca do startowego zestawu 6 zgłoszeń przykładowych
    private async void OnRestoreSamplesClicked(object? sender, EventArgs e)
    {
        try
        {
            await SampleReports.RestoreAsync(_repository, DateTimeOffset.Now);
            await ReportsList.OnNavigatedToAsync(null);
            SampleDataStatus.Text = "Przywrócono przykłady – wróć na listę.";
        }
        catch (Exception ex)
        {
            Errors.Handle(ex);
        }
    }
}
