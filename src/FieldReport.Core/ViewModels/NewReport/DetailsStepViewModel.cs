using CommunityToolkit.Mvvm.ComponentModel;
using FieldReport.Core.Models;
using FieldReport.Core.Presentation;
using FieldReport.Core.Services;
using FieldReport.Core.ValueObjects;

namespace FieldReport.Core.ViewModels.NewReport;

/// <summary>Krok „Dane”: pole → reguła z modelu (ReportTitle.Create) → stan widoku. Sam nie zna reguły 3–80 znaków.</summary>
// TODO: dodaj parametr konstruktora IErrorMessageProvider messages (przed localizer)
public sealed partial class DetailsStepViewModel(ILocalizer localizer) : WizardStepViewModel
{
    private NewReportDraft _draft = null!;

    [ObservableProperty]
    private string _titleInput = string.Empty;

    [ObservableProperty]
    private string? _titleError;

    [ObservableProperty]
    private string _descriptionInput = string.Empty;

    [ObservableProperty]
    private PriorityOption? _selectedPriority;

    public int TitleMaxLength => ReportTitle.MaxLength;
    public int DescriptionMaxLength => Report.MaxDescriptionLength;

    public IReadOnlyList<PriorityOption> Priorities { get; } =
        Enum.GetValues<ReportPriority>().Select(p => new PriorityOption(p, localizer.GetString($"ReportPriority_{p}"))).ToList();

    public override string Title => localizer.GetString("NewReport_DetailsTitle");

    public override void Attach(NewReportDraft draft)
    {
        _draft = draft;
        SelectedPriority = Priorities.First(p => p.Priority == draft.Priority); // domyślnie Normalny
    }

    partial void OnTitleInputChanged(string value)
    {
        // TODO: ReportTitle.Create(value) → _draft.Title (null przy błędzie), IsValid,
        // TitleError = komunikat błędu (messages.GetMessage), ale nie dla pustego pola
    }

    partial void OnDescriptionInputChanged(string value) => _draft.Description = value;

    // Picker przy ponownym wyświetleniu kroku (nowy widok z szablonu) potrafi wysłać null — wtedy wracamy do wartości z draftu
    partial void OnSelectedPriorityChanged(PriorityOption? value)
    {
        if (value is null) SelectedPriority = Priorities.First(p => p.Priority == _draft.Priority);
        else _draft.Priority = value.Priority;
    }
}
