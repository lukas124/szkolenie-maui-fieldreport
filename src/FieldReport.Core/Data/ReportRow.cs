using SQLite;

namespace FieldReport.Core.Data;

/// <summary>
/// Wiersz tabeli <c>reports</c>. Wiersz ≠ model: typy proste (int zamiast enum), brak reguł;
/// zamiana w <see cref="ReportRowMapper"/>. W bazie są tylko zgłoszenia i zmiany czekające na wysłanie do API.
/// </summary>
[Table("reports")]
public sealed class ReportRow
{
    [PrimaryKey] public Guid Id { get; set; }            // lokalne Id (po wysłaniu: Id z serwera)
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Priority { get; set; }                    // enum zapisany jako liczba
    public int Status { get; set; }
    public string? PhotoPath { get; set; }               // ścieżka pliku, nie BLOB
    public DateTimeOffset CreatedAt { get; set; }
    [Indexed] public DateTimeOffset UpdatedAt { get; set; } // sortowanie listy
    public bool IsOnServer { get; set; }                 // migracja 2: zgłoszenie z serwera ze zmianą do wysłania
    public Guid? IdempotencyKey { get; set; }            // migracja 3: klucz nagłówka Idempotency-Key (nowe zgłoszenie)
}
