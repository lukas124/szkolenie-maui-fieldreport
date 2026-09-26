using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using FieldReport.Core.Navigation;
using FieldReport.Core.Presentation;
using FieldReport.Core.Services;

namespace FieldReport.Core.ViewModels;

/// <summary>Lista zgłoszeń. Tylko orkiestracja: usługa → fabryka → widok / komunikat.</summary>
public sealed partial class ReportsViewModel(
    INavigationService navigation,
    IReportService reports,
    IReportItemFactory items,
    IUserFeedback feedback) : BaseViewModel(navigation)
{
    public ObservableCollection<ReportItem> Reports { get; } = new(); // elementy listy

    public override async Task OnNavigatedToAsync(object? parameter)
    {
        var result = await reports.GetAllAsync();            // 1) usługa
        if (!result.IsSuccess)
        {
            await feedback.ShowErrorAsync(result.Error!);    // 2a) błąd → komunikat
            return;
        }

        Reports.ReplaceWith(result.Value!.Select(items.Create)); // 2b) fabryka → lista
    }
}
