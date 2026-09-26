using FieldReport.Core.Models;

namespace FieldReport.Core.Presentation;

/// <summary>Model prezentacji szczegółów zgłoszenia: gotowe teksty + lista akcji zbudowana z <see cref="Report.AllowedNextStatuses"/>.
/// <c>PhotoSource</c> = plik w telefonie albo adres zdjęcia z API.</summary>
public sealed record ReportDetails(
    Guid Id, string Title, string DescriptionText,
    ReportPriority Priority, string PriorityText,
    ReportStatus Status, string StatusText,
    string CreatedAtText, string UpdatedAtText,
    bool IsPendingSync, string SyncStateText, string? PhotoSource,
    IReadOnlyList<StatusAction> Actions);
