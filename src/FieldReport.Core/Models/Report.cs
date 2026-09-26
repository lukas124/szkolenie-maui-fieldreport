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
        DateTimeOffset updatedAt,
        bool isOnServer)
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
        IsOnServer = isOnServer;
    }

    /// <summary>Nowe zgłoszenie założone w aplikacji: status Nowe, „Oczekuje na wysłanie”.</summary>
    public static Report CreateNew(Guid id, ReportTitle title, string? description, ReportPriority priority, DateTimeOffset now)
        => new(id, title, description, priority, ReportStatus.New, SyncState.Pending, null, now, now, isOnServer: false);

    public Guid Id { get; }
    public ReportTitle Title { get; }
    public string? Description { get; }
    public ReportPriority Priority { get; }
    public string? PhotoPath { get; }
    public DateTimeOffset CreatedAt { get; }

    public ReportStatus Status { get; private set; }
    public SyncState SyncState { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>
    /// Czy zgłoszenie choć raz trafiło do API. „Oczekuje na wysłanie” (Pending) może oznaczać nowe zgłoszenie
    /// albo wysłane zgłoszenie z lokalnymi zmianami (np. statusu) — to drugie istnieje już na serwerze.
    /// </summary>
    public bool IsOnServer { get; private set; }

    /// <summary>Reguła usuwania z listy zgłoszeń: tylko zgłoszenia, które nigdy nie zostały wysłane (usunięcie lokalne nie rozjedzie się z serwerem).</summary>
    public bool CanDelete => !IsOnServer;

    /// <summary>Reguła przejść statusu w szczegółach zgłoszenia: Nowe → W trakcie → Zakończone. Mieszka w modelu, nie w ViewModelu ani XAML.</summary>
    public IReadOnlyList<ReportStatus> AllowedNextStatuses => Status switch
    {
        // Nowe → W trakcie
        ReportStatus.New => [ReportStatus.InProgress],
        // W trakcie → Zakończone
        ReportStatus.InProgress => [ReportStatus.Done],
        // Zakończone → nic
        _ => []
    };

    public Result ChangeStatus(ReportStatus next, DateTimeOffset now)
    {
        if (!AllowedNextStatuses.Contains(next)) return Result.Failure(Errors.InvalidStatusTransition);
        Status = next;
        UpdatedAt = now;
        // zmiana działa offline: zapis lokalny + oznaczenie do wysłania
        SyncState = SyncState.Pending;
        return Result.Success();
    }
}
