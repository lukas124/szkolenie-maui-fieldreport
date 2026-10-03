using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using FieldReport.Core.Models;
using FieldReport.Core.Navigation;
using FieldReport.Core.Presentation;
using FieldReport.Core.Services;

namespace FieldReport.Core.ViewModels.NewReport;

/// <summary>
/// Kreator nowego zgłoszenia (modal, jedna strona). Przepływ orkiestruje kroki, zapis i nawigację:
/// nie zna reguł tytułu (model), nie liczy indeksów ręcznie (StepSequence), nie wie, jak zapisać (usługa)
/// ani jak wygląda krok (NewReportStepSelector w App). Nowy krok nie zmienia logiki przepływu.
/// </summary>
public sealed partial class NewReportFlowViewModel : BaseViewModel
{
    private readonly IReportService _reports;
    private readonly IUserFeedback _feedback;
    private readonly NewReportDraft _draft = new();
    private readonly StepSequence _sequence;

    public NewReportFlowViewModel(
        INavigationService navigation, IReportService reports, IUserFeedback feedback,
        DetailsStepViewModel details, PhotoStepViewModel photo, SummaryStepViewModel summary)
        : base(navigation)
    {
        _reports = reports;
        _feedback = feedback;
        Steps = [details, photo, summary];               // kolejność kroków
        _sequence = new StepSequence(Steps.Count);
        foreach (var step in Steps)
        {
            step.Attach(_draft);                         // wspólny draft
            step.ValidityChanged += (_, _) => NextCommand.NotifyCanExecuteChanged();
        }
        Show();                                          // pokaż krok 1
    }

    private IReadOnlyList<WizardStepViewModel> Steps { get; }

    // MAUI nie ma ContentControl: bieżący krok jako jednoelementowa kolekcja dla BindableLayout
    public ObservableCollection<WizardStepViewModel> CurrentStepHost { get; } = new();
    private WizardStepViewModel CurrentStep => Steps[_sequence.Index];
    public double Progress => _sequence.Progress;
    public int StepNumber => _sequence.Index + 1;
    public int StepCount => _sequence.Count;
    public bool IsFirstStep => _sequence.IsFirst;
    public bool IsLastStep => _sequence.IsLast;

    private void Show()
    {
        CurrentStepHost.Clear();
        CurrentStepHost.Add(CurrentStep); // szablon z selektora
        CurrentStep.OnShown();
        Title = CurrentStep.Title;
        OnPropertyChanged(nameof(Progress));
        OnPropertyChanged(nameof(StepNumber));
        OnPropertyChanged(nameof(IsFirstStep));
        OnPropertyChanged(nameof(IsLastStep));
        NextCommand.NotifyCanExecuteChanged();
    }

    private bool CanGoNext() => CurrentStep.IsValid;

    [RelayCommand(CanExecute = nameof(CanGoNext))]
    private async Task NextAsync(CancellationToken ct)
    {
        if (_sequence.MoveNext()) { Show(); return; }  // dalej

        var result = await _reports.CreateAsync(_draft, ct);
        if (!result.IsSuccess) { await _feedback.ShowErrorAsync(result.Error!); return; }

        await Navigation.ResetToAsync<ReportsViewModel>(); // lista
    }

    // Przycisk „Wstecz” w stopce kreatora = to samo co systemowy przycisk wstecz
    [RelayCommand]
    private Task BackAsync() => OnBackRequestedAsync();

    public override async Task OnBackRequestedAsync() // wstecz
    {
        if (_sequence.MovePrevious()) { Show(); return; }
        await OnCloseRequestedAsync(); // krok 1: jak „X”
    }

    // „X” w nagłówku: porzucenie zgłoszenia na dowolnym kroku (po potwierdzeniu)
    public override async Task OnCloseRequestedAsync()
    {
        if (await _feedback.ConfirmAsync("NewReport_ExitTitle", "NewReport_ExitMessage"))
            await Navigation.GoBackAsync(); // zamyka modal
    }
}
