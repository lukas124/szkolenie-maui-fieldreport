using FieldReport.Core.Models;

namespace FieldReport.Core.Presentation;

/// <summary>
/// Model prezentacji elementu listy: gotowy do wyświetlenia (teksty już sformatowane).
/// <c>ThumbnailSource</c> = ścieżka pliku w telefonie albo adres zdjęcia z API (Image przyjmie jedno i drugie).
/// </summary>
public sealed record ReportItem(
    Guid Id, string Title, string CreatedAtText, ReportPriority Priority, string PriorityText,
    ReportStatus Status, string StatusText, bool IsPendingSync, string? ThumbnailSource, bool CanDelete);
