using FieldReport.Core.ValueObjects;

namespace FieldReport.Core.Models;

/// <summary>
/// Stan przepływu kreatora nowego zgłoszenia: dane zbierane przez kolejne kroki. Kroki piszą, usługa czyta przy zapisie.
/// Tytuł jest już zwalidowany (<see cref="ReportTitle"/>), więc null = krok „Dane” niekompletny.
/// </summary>
public sealed class NewReportDraft
{
    public ReportTitle? Title { get; set; }
    public string? Description { get; set; }
    public ReportPriority Priority { get; set; } = ReportPriority.Normal;
    public string? PhotoPath { get; set; }
}
