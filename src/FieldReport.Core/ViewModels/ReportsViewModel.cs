using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using FieldReport.Core.Navigation;
using FieldReport.Core.Presentation;
using FieldReport.Core.Services;
using FieldReport.Core.Services.Push;
using FieldReport.Core.ViewModels.NewReport;

namespace FieldReport.Core.ViewModels;

/// <summary>Lista zgłoszeń. Tylko orkiestracja: usługa → fabryka → widok / komunikat / nawigacja.</summary>
public sealed partial class ReportsViewModel(
    INavigationService navigation,
    IReportService reports,
    IReportItemFactory items,
    IUserFeedback feedback,
    IMessenger messenger) : BaseViewModel(navigation), IRecipient<ReportsChangedMessage>
{
    private bool _loaded;

    public ObservableCollection<ReportItem> Reports { get; } = new(); // elementy listy

    public override Task OnNavigatedToAsync(object? parameter)
    {
        // WeakReferenceMessenger: rejestracja nie trzyma ViewModelu przy życiu (brak wycieku jak przy zdarzeniu singletona)
        messenger.Register(this);
        return LoadAsync(); // jednorazowe ładowanie
    }

    // powiadomienie push na pierwszym planie → odświeżenie listy (wołane na wątku UI)
    public void Receive(ReportsChangedMessage message) => _ = RefreshFromMessageAsync();

    private async Task RefreshFromMessageAsync()
    {
        try { await LoadAsync(); }
        catch (Exception) { /* ignorujemy: to odświeżenie w tle, błąd pokaże się przy następnym ładowaniu listy */ }
    }

    // Powrót na listę (ze szczegółów, z kreatora, z Ustawień po zmianie języka): odświeżamy ją. Pierwsze pojawienie się pomijamy.
    public override Task OnAppearingAsync() => _loaded ? LoadAsync() : Task.CompletedTask;

    private async Task LoadAsync()
    {
        var result = await reports.GetAllAsync();            // 1) usługa
        if (!result.IsSuccess)
        {
            await feedback.ShowErrorAsync(result.Error!);    // 2a) błąd → komunikat
            return;
        }

        Reports.ReplaceWith(result.Value!.Select(items.Create)); // 2b) fabryka → lista
        _loaded = true;
    }

    // [RelayCommand] na metodzie zwracającej Task → AsyncRelayCommand:
    // dopóki zwrócony Task trwa, komenda ma CanExecute = false,
    // więc drugie tapnięcie nic nie robi
    [RelayCommand]
    private Task OpenReportAsync(ReportItem item) =>
        Navigation.NavigateToAsync<ReportDetailsViewModel>(item.Id);

    // Lista zgłoszeń: „+” otwiera kreator (modal), ikona w nagłówku — Ustawienia
    [RelayCommand]
    private Task NewReportAsync() => Navigation.OpenModalAsync<NewReportFlowViewModel>();

    [RelayCommand]
    private Task OpenSettingsAsync() => Navigation.NavigateToAsync<SettingsViewModel>();

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

    private static bool CanDelete(ReportItem? item) => item?.CanDelete == true; // decyzja widoku: pokazać akcję? (reguła w Report.CanDelete)
}
