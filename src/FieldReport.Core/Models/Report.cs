using FieldReport.Core.ValueObjects;

namespace FieldReport.Core.Models;

/// <summary>Zgłoszenie serwisowe. Model pilnuje swoich reguł biznesowych.</summary>
public sealed class Report
{
    public Report(
        Guid id,
        ReportTitle title,
        string? description,
        ReportPriority priority,
        ReportStatus status,
        SyncState syncState,
        string? photoPath,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt)
    {
        Id = id;
        Title = title ?? throw new ArgumentNullException(nameof(title));
        Description = description;
        Priority = priority;
        Status = status;
        SyncState = syncState;
        PhotoPath = photoPath;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    /// <summary>Nowe zgłoszenie założone w aplikacji: status Nowe, „Oczekuje na wysłanie”.</summary>
    public static Report CreateNew(Guid id, ReportTitle title, string? description, ReportPriority priority, DateTimeOffset now)
        => new(id, title, description, priority, ReportStatus.New, SyncState.Pending, null, now, now);

    public Guid Id { get; }
    public ReportTitle Title { get; }
    public string? Description { get; }
    public ReportPriority Priority { get; }
    public string? PhotoPath { get; }
    public DateTimeOffset CreatedAt { get; }

    public ReportStatus Status { get; private set; }
    public SyncState SyncState { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
}
