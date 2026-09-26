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
    [ObservableProperty]
    // gotowe teksty + lista akcji (StatusAction) zbudowana z AllowedNextStatuses
    private ReportDetails? _report;

    // Parametr nawigacji = Id zgłoszenia
    // (ReportsViewModel: NavigateToAsync<ReportDetailsViewModel>(item.Id))
    public override async Task OnNavigatedToAsync(object? parameter)
    {
        var reportId = (Guid)parameter!;
        // usługa
        var result = await reports.GetAsync(reportId);
        // wynik → ekran albo komunikat
        await ShowAsync(result);
    }

    [RelayCommand]
    private async Task ChangeStatusAsync(StatusAction action)
    {
        // usługa: reguła z modelu + zapis
        var result = await reports.ChangeStatusAsync(Report!.Id, action.TargetStatus);
        await ShowAsync(result);
    }

    // Wynik operacji → nowe szczegóły na ekranie albo komunikat błędu
    private async Task ShowAsync(Result<Report> result)
    {
        if (result.IsSuccess)
            Report = details.Create(result.Value!); // nowy stan ekranu
        else
            await feedback.ShowErrorAsync(result.Error!);
    }
}
