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
        bool isOnServer,
        string? photoUrl = null,
        Guid? idempotencyKey = null)
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
        PhotoUrl = photoUrl;
        IdempotencyKey = idempotencyKey;
    }

    /// <summary>Maksymalna długość opisu (kreator nowego zgłoszenia: opis opcjonalny, maks. 500 znaków).</summary>
    public const int MaxDescriptionLength = 500;

    /// <summary>
    /// Nowe zgłoszenie założone w aplikacji: status Nowe, „Oczekuje na wysłanie”. Klucz idempotencji powstaje tu
    /// raz i trafia do wiersza SQLite — wysyłka (POST) wysyła go przy każdej próbie.
    /// </summary>
    public static Report CreateNew(Guid id, ReportTitle title, string? description, ReportPriority priority, DateTimeOffset now,
        string? photoPath = null)
        => new(id, title, description, priority, ReportStatus.New, SyncState.Pending, photoPath, now, now, isOnServer: false,
            idempotencyKey: Guid.NewGuid());

    /// <summary>Id zgłoszenia: w telefonie lokalne, po wysłaniu (POST) — nadane przez serwer.</summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Klucz do nagłówka <c>Idempotency-Key</c> przy wysyłce nowego zgłoszenia (POST). Ten sam przy każdej próbie,
    /// więc serwer nie utworzy duplikatu po zgubionej odpowiedzi. Null = zgłoszenie z serwera (wysyłane przez PUT).
    /// </summary>
    public Guid? IdempotencyKey { get; }
    public ReportTitle Title { get; }
    public string? Description { get; }
    public ReportPriority Priority { get; }
    /// <summary>Zdjęcie zapisane w telefonie, czekające na wysłanie (ścieżka pliku).</summary>
    public string? PhotoPath { get; }

    /// <summary>Zdjęcie na serwerze: podpisany adres z API (ważny ograniczony czas, nie zapisujemy go w bazie).</summary>
    public string? PhotoUrl { get; }
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

    /// <summary>
    /// Zgłoszenie przyjęte przez API (POST): od teraz ma Id nadane przez serwer, zmiany wysyłamy przez PUT
    /// i zgłoszenia nie wolno usunąć w telefonie.
    /// </summary>
    public void MarkOnServer(Guid serverId)
    {
        Id = serverId;
        IsOnServer = true;
    }

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
        // zmiana czeka na wysłanie
        // (zapis w telefonie, wysyłka przy synchronizacji)
        SyncState = SyncState.Pending;
        return Result.Success();
    }
}
