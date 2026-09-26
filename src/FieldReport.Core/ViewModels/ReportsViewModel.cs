using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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

    // Komenda jako scenariusz: potwierdź → wykonaj (usługa) → pokaż wynik → zaktualizuj widok
    [RelayCommand(CanExecute = nameof(CanDelete))]
    private async Task DeleteReportAsync(ReportItem item, CancellationToken ct)
    {
        if (!await feedback.ConfirmAsync("Reports_DeleteTitle", "Reports_DeleteMessage"))
            return;                                          // 1) użytkownik powiedział „Nie”

        var result = await reports.DeleteAsync(item.Id, ct); // 2) usługa (sprawdza regułę)
        if (!result.IsSuccess)
        {
            await feedback.ShowErrorAsync(result.Error!);    // 3a) błąd → komunikat
            return;
        }

        Reports.Remove(item);                                // 3b) sukces → karta znika
    }

    private static bool CanDelete(ReportItem? item) => item?.IsPendingSync == true; // decyzja widoku: pokazać akcję?
}
