using CommunityToolkit.Mvvm.ComponentModel;
using FieldReport.Core.Models;
using FieldReport.Core.Presentation;
using FieldReport.Core.Services;
using FieldReport.Core.ValueObjects;

namespace FieldReport.Core.ViewModels.NewReport;

/// <summary>Krok „Podsumowanie”: wszystkie dane z draftu, teksty z formattera. „Zapisz” obsługuje przepływ.</summary>
public sealed partial class SummaryStepViewModel : WizardStepViewModel
{
    private readonly IReportFormatter _formatter;
    private readonly ILocalizer _localizer;
    private NewReportDraft _draft = null!;

    public SummaryStepViewModel(IReportFormatter formatter, ILocalizer localizer)
    {
        _formatter = formatter;
        _localizer = localizer;
        IsValid = true;
    }

    [ObservableProperty]
    private string _reportTitle = string.Empty;

    [ObservableProperty]
    private string _descriptionText = string.Empty;

    [ObservableProperty]
    private string _priorityText = string.Empty;

    [ObservableProperty]
    private string? _photoPath;

    public override string Title => _localizer.GetString("NewReport_SummaryTitle");

    public override void Attach(NewReportDraft draft) => _draft = draft;

    // Dane mogły się zmienić w poprzednich krokach — odświeżamy przy każdym pokazaniu
    public override void OnShown()
    {
        ReportTitle = _draft.Title?.Value ?? string.Empty;
        DescriptionText = string.IsNullOrWhiteSpace(_draft.Description) ? _localizer.GetString("Details_NoDescription") : _draft.Description.Trim();
        PriorityText = _formatter.FormatPriority(_draft.Priority);
        PhotoPath = _draft.PhotoPath;
    }
}
