using FieldReport.Core.Models;

namespace FieldReport.Core.Presentation;

/// <summary>Akcja zmiany statusu wyświetlana jako przycisk (np. „Rozpocznij” → W trakcie).</summary>
public sealed record StatusAction(ReportStatus TargetStatus, string Text);
