using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using FieldReport.Core.Navigation;
using FieldReport.Core.Presentation;
using FieldReport.Core.Services;
using FieldReport.Core.Services.Push;
using FieldReport.Core.Services.Sync;
using FieldReport.Core.ViewModels.NewReport;

namespace FieldReport.Core.ViewModels;

/// <summary>Lista zgłoszeń. Tylko orkiestracja: usługa → fabryka → widok / komunikat / nawigacja.</summary>
public sealed partial class ReportsViewModel(
    INavigationService navigation,
    IReportService reports,
    IReportItemFactory items,
    IUserFeedback feedback,
    IMessenger messenger,
    ISyncService sync) : BaseViewModel(navigation), IRecipient<ReportsChangedMessage>
{
    private bool _loaded;

    public ObservableCollection<ReportItem> Reports { get; } = new(); // elementy listy

    [ObservableProperty]
    private bool _isRefreshing;

    // true = serwer nieosiągalny: na liście są tylko zgłoszenia czekające w telefonie (widok pokazuje pasek z informacją)
    [ObservableProperty]
    private bool _isOffline;

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

    // Lista = zgłoszenia z API + niewysłane z telefonu (offline: tylko niewysłane i IsOffline = true)
    private async Task LoadAsync()
    {
        var list = await reports.GetAllAsync();
        Reports.ReplaceWith(list.Reports.Select(items.Create));
        IsOffline = list.IsOffline;
        _loaded = true;
    }

    // Lista zgłoszeń i synchronizacja: pull-to-refresh = wysłanie zgłoszeń czekających w telefonie + ponowne pobranie listy
    [RelayCommand]
    private async Task RefreshAsync(CancellationToken ct)
    {
        IsRefreshing = true;        // wskaźnik „odświeżam”
        try
        {
            await sync.SyncAsync(ct); // usługa: wyślij zaległe
            await LoadAsync();        // usługa → fabryka → lista
        }
        finally
        {
            IsRefreshing = false;
        }
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
