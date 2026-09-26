using FieldReport.Core.Models;
using FieldReport.Core.Services;
using SQLite;

namespace FieldReport.Core.Data;

/// <summary>
/// Repozytorium SQLite (singleton): leniwe otwarcie bazy przy pierwszym użyciu, migracje.
/// InsertOrReplace + Guid od aplikacji = idempotentny zapis.
/// </summary>
public sealed class SqliteReportRepository(IAppPaths paths) : IReportRepository
{
    public const string FileName = "fieldreport.db3";

    private readonly SemaphoreSlim _initLock = new(1, 1);
    private SQLiteAsyncConnection? _db;

    // Zwraca połączenie z bazą; każda metoda repozytorium wywołuje ją na początku.
    // SQLiteAsyncConnection (sqlite-net) = połączenie z plikiem bazy fieldreport.db3; wszystkie operacje
    //   są asynchroniczne (…Async, await), więc zapytanie nie blokuje wątku UI.
    // SemaphoreSlim(1, 1) = blokada dla kodu async: wpuszcza jedno wywołanie naraz, drugie czeka w WaitAsync,
    //   aż pierwsze zrobi Release.
    // Leniwe otwarcie: bazę otwieramy i migrujemy przy pierwszym użyciu, nie w konstruktorze —
    //   konstruktor nie może czekać (await) na otwarcie pliku i migracje, a utworzenie repozytorium przez DI
    //   nie dotyka dysku.
    private async Task<SQLiteAsyncConnection> DbAsync()
    {
        if (_db is not null) return _db; // baza już otwarta: zwracamy ją bez blokady

        await _initLock.WaitAsync(); // dwa pierwsze zapytania naraz nie otworzą bazy dwa razy
        try
        {
            if (_db is not null) return _db; // bazę otworzyło wywołanie, na które czekaliśmy
            var db = new SQLiteAsyncConnection(Path.Combine(paths.DataDirectory, FileName),
                SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.SharedCache);
            await SqliteMigrations.MigrateAsync(db); // tworzy/uzupełnia tabele do bieżącej wersji schematu
            return _db = db;
        }
        finally
        {
            _initLock.Release(); // wpuszcza następne czekające wywołanie
        }
    }

    public async Task<IReadOnlyList<Report>> GetAllAsync(CancellationToken ct = default)
    {
        var db = await DbAsync();
        var rows = await db.Table<ReportRow>().OrderByDescending(r => r.UpdatedAt).ToListAsync();
        return rows.Select(ReportRowMapper.ToModel).ToList(); // wiersz → model
    }

    public async Task<Report?> GetAsync(Guid id, CancellationToken ct = default)
    {
        var db = await DbAsync();
        var row = await db.FindAsync<ReportRow>(id);
        return row is null ? null : ReportRowMapper.ToModel(row);
    }

    public async Task SaveAsync(Report report, CancellationToken ct = default)
    {
        var db = await DbAsync();
        await db.InsertOrReplaceAsync(ReportRowMapper.ToRow(report)); // model → wiersz
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var db = await DbAsync();
        await db.DeleteAsync<ReportRow>(id);
    }

    // Usunięcie starego wiersza i zapis nowego w jednej transakcji: inne zapytania na tym połączeniu czekają do jej końca
    public async Task ReplaceAsync(Guid oldId, Report report, CancellationToken ct = default)
    {
        var db = await DbAsync();
        await db.RunInTransactionAsync(tx =>
        {
            tx.Delete<ReportRow>(oldId);
            tx.InsertOrReplace(ReportRowMapper.ToRow(report)); // model → wiersz
        });
    }

    public async Task DeleteAllAsync(CancellationToken ct = default)
    {
        var db = await DbAsync();
        await db.DeleteAllAsync<ReportRow>();
    }
}
