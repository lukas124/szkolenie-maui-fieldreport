using SQLite;

namespace FieldReport.Core.Data;

/// <summary>
/// Migracje schematu bazy. Numer wersji schematu jest zapisany w samej bazie (<c>PRAGMA user_version</c>);
/// przy starcie wykonujemy tylko kroki, których dana baza jeszcze nie ma. Nowa zmiana schematu = nowy krok
/// na końcu listy (starych kroków nigdy nie zmieniamy — telefony użytkowników mają je już wykonane).
/// </summary>
public static class SqliteMigrations
{
    // krok N tablicy = migracja nr N+1
    private static readonly Func<SQLiteAsyncConnection, Task>[] Steps =
    [
        // 1: tabela zgłoszeń
        db => db.CreateTableAsync<ReportRow>(),

        // 2: kolumna IsOnServer (CreateTableAsync dopisuje brakujące kolumny). Istniejące wiersze dostają false —
        //    to prawda, bo wcześniej żadne zgłoszenie nie było wysyłane do API
        db => db.CreateTableAsync<ReportRow>(),

        // 3: kolumna IdempotencyKey (klucz nagłówka Idempotency-Key dla POST). Niewysłane nowe zgłoszenia zapisane
        //    przed tą wersją dostają klucz od razu: randomblob(16) = 16 losowych bajtów, hex = Guid bez kresek
        async db =>
        {
            await db.CreateTableAsync<ReportRow>();
            await db.ExecuteAsync("UPDATE reports SET IdempotencyKey = lower(hex(randomblob(16))) WHERE IsOnServer = 0");
        },
    ];

    public static int LatestVersion => Steps.Length;

    public static async Task MigrateAsync(SQLiteAsyncConnection db)
    {
        // nowa baza ma user_version = 0
        var version = await db.ExecuteScalarAsync<int>("PRAGMA user_version");
        for (var step = version; step < Steps.Length; step++)   // brakujące kroki
        {
            await Steps[step](db);
            await db.ExecuteAsync($"PRAGMA user_version = {step + 1}");
        }
    }
}
