using FieldReport.Core.Models;

namespace FieldReport.Core.ViewModels.NewReport;

/// <summary>Pozycja listy priorytetów (Picker wyświetla ToString()).</summary>
public sealed record PriorityOption(ReportPriority Priority, string Text)
{
    public override string ToString() => Text;
}
