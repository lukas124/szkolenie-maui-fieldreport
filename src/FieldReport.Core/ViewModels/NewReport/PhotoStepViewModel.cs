using FieldReport.Core.Models;
using FieldReport.Core.Services;

namespace FieldReport.Core.ViewModels.NewReport;

/// <summary>Krok „Zdjęcie” (zdjęcie opcjonalne): „Dalej” jest zawsze aktywne.</summary>
public sealed class PhotoStepViewModel : WizardStepViewModel
{
    private readonly ILocalizer _localizer;

    public PhotoStepViewModel(ILocalizer localizer)
    {
        _localizer = localizer;
        IsValid = true; // krok opcjonalny: „Dalej” zawsze aktywne
    }

    public override string Title => _localizer.GetString("NewReport_PhotoTitle");

    public override void Attach(NewReportDraft draft) { }
}
