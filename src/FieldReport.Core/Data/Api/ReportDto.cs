using FieldReport.Core.Models;

namespace FieldReport.Core.Data.Api;

/// <summary>Kontrakt API (JSON camelCase, enumy jako tekst). <c>hasPhoto</c>/<c>photoUrl</c> ustawia serwer;
/// <c>photoUrl</c> to podpisany adres zdjęcia, który można pobrać bez tokenu (ważny ograniczony czas).</summary>
public sealed record ReportDto(
    Guid Id,
    string Title,
    string? Description,
    ReportPriority Priority,
    ReportStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    bool HasPhoto = false,
    string? PhotoUrl = null);
