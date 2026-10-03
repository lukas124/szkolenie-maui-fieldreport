using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FieldReport.Core.Models;
using FieldReport.Core.Navigation;
using FieldReport.Core.Presentation;
using FieldReport.Core.Services;

namespace FieldReport.Core.ViewModels;

/// <summary>Szczegóły zgłoszenia: reguła zmiany statusu jest w modelu, ViewModel tylko orkiestruje.</summary>
public sealed partial class ReportDetailsViewModel(
    INavigationService navigation,
    IReportService reports,
    IReportDetailsFactory details,
    IUserFeedback feedback) : BaseViewModel(navigation)
{
    private Report? _model; // wyświetlane zgłoszenie (model) — potrzebne przy zmianie statusu, także offline

    [ObservableProperty]
    // gotowe teksty + lista akcji (StatusAction) zbudowana z AllowedNextStatuses
    private ReportDetails? _report;

    // Parametr nawigacji = Id zgłoszenia (z listy albo z tapniętego powiadomienia)
    public override async Task OnNavigatedToAsync(object? parameter)
    {
        var reportId = (Guid)parameter!;
        // usługa: z telefonu (niewysłane) albo z API
        var result = await reports.GetAsync(reportId);
        if (result.Error == Errors.ReportNotFound)
        {
            // Najczęściej: lista pokazywała jeszcze niewysłane zgłoszenie z lokalnym Id, a w tle zostało ono wysłane
            // i ma już Id nadane przez serwer. Mówimy, co się stało, i wracamy na listę — odświeży się sama (OnAppearingAsync)
            await feedback.ShowInfoAsync("Details_NotFound_Title", "Details_NotFound_Message");
            await Navigation.GoBackAsync();
            return;
        }
        // wynik → ekran albo komunikat
        await ShowAsync(result);
    }

    [RelayCommand]
    private async Task ChangeStatusAsync(StatusAction action)
    {
        var result = await reports.ChangeStatusAsync(_model!, action.TargetStatus); // usługa: reguła z modelu + zapis
        await ShowAsync(result);
    }

    // Wynik operacji → nowe szczegóły na ekranie albo komunikat błędu
    private async Task ShowAsync(Result<Report> result)
    {
        if (result.IsSuccess)
        {
            _model = result.Value!;
            Report = details.Create(_model); // nowy stan ekranu
        }
        else await feedback.ShowErrorAsync(result.Error!);
    }
}
