using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FieldReport.Core.Data;
using FieldReport.Core.Navigation;
using FieldReport.Core.Services;

namespace FieldReport.Core.ViewModels.Playground;

/// <summary>ViewModel strony Playground: deleguje do ViewModelu komponentu (licznik) i ma narzędzia dewelopera do danych przykładowych.</summary>
public sealed partial class PlaygroundViewModel(
    INavigationService navigation,
    CounterViewModel counter,
    IReportRepository repository,
    IClock clock) : BaseViewModel(navigation)
{
    public CounterViewModel Counter { get; } = counter;

    [ObservableProperty]
    private string _sampleDataStatus = "Zmianę zobaczysz po powrocie na listę."; // komunikat pod nagłówkiem „dane przykładowe”

    // Narzędzia dewelopera działają prosto na repozytorium, z pominięciem reguły „wysłanych nie usuwamy” z ReportService.
    // Lista odświeża się sama po powrocie (ReportsViewModel.OnAppearingAsync).

    // „Wyczyść listę”: usuwa wszystkie zgłoszenia, także wysłane
    [RelayCommand]
    private async Task ClearReportsAsync()
    {
        await SampleReports.RemoveAllAsync(repository);
        SampleDataStatus = "Lista wyczyszczona – wróć na listę.";
    }

    // „Przywróć przykłady”: wraca do startowego zestawu 6 zgłoszeń przykładowych
    [RelayCommand]
    private async Task RestoreSamplesAsync()
    {
        await SampleReports.RestoreAsync(repository, clock.Now);
        SampleDataStatus = "Przywrócono przykłady – wróć na listę.";
    }
}
