using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using FieldReport.Core.Navigation;
using FieldReport.Core.Presentation;
using FieldReport.Core.Services;

namespace FieldReport.Core.ViewModels;

/// <summary>Lista zgłoszeń. Tylko orkiestracja: usługa → fabryka → widok / komunikat.</summary>
// TODO: dodaj parametry konstruktora (po navigation): IReportService reports, IReportItemFactory items, IUserFeedback feedback
public sealed partial class ReportsViewModel(INavigationService navigation) : BaseViewModel(navigation)
{
    public ObservableCollection<ReportItem> Reports { get; } = new(); // elementy listy

    public override Task OnNavigatedToAsync(object? parameter)
    {
        // TODO: pobierz zgłoszenia (reports.GetAllAsync); błąd → feedback.ShowErrorAsync(result.Error!),
        // sukces → Reports.ReplaceWith(result.Value!.Select(items.Create))
        return Task.CompletedTask;
    }
}
