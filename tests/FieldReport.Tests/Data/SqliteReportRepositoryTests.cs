using FieldReport.Core.Data;
using FieldReport.Core.Models;
using FieldReport.Core.Services;
using FieldReport.Core.ValueObjects;
using FieldReport.Tests.TestData;
using SQLite;

namespace FieldReport.Tests.Data;

/// <summary>Prawdziwy SQLite w katalogu tymczasowym (bez emulatora) — sprawdza mapowanie, trwałość i migracje.</summary>
[TestFixture]
public class SqliteReportRepositoryTests
{
    private string _dir = null!;
    private IAppPaths _paths = null!;

    [SetUp]
    public void SetUp()
    {
        _dir = Path.Combine(Path.GetTempPath(), "fieldreport-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_dir);
        _paths = Mock.Of<IAppPaths>(p => p.DataDirectory == _dir);
    }

    [TearDown]
    public void TearDown()
    {
        SQLiteAsyncConnection.ResetPool(); // zamyka połączenia, żeby dało się usunąć plik
        try { Directory.Delete(_dir, recursive: true); } catch (IOException) { }
    }

    [Test]
    public async Task Save_ThenGet_RoundTripsAllFields()
    {
        var repository = new SqliteReportRepository(_paths);
        var report = new Report(Guid.NewGuid(), ReportTitle.Create("Awaria pompy").Value!, "Opis", ReportPriority.High,
            ReportStatus.InProgress, SyncState.Pending, "/photos/a.jpg", TestReports.Now, TestReports.Now.AddMinutes(5), isOnServer: false);

        await repository.SaveAsync(report);
        var loaded = await repository.GetAsync(report.Id);

        Assert.Multiple(() =>
        {
            Assert.That(loaded, Is.Not.Null);
            Assert.That(loaded!.Title.Value, Is.EqualTo("Awaria pompy"));
            Assert.That(loaded.Description, Is.EqualTo("Opis"));
            Assert.That(loaded.Priority, Is.EqualTo(ReportPriority.High));
            Assert.That(loaded.Status, Is.EqualTo(ReportStatus.InProgress));
            Assert.That(loaded.SyncState, Is.EqualTo(SyncState.Pending));
            Assert.That(loaded.PhotoPath, Is.EqualTo("/photos/a.jpg"));
            Assert.That(loaded.CreatedAt, Is.EqualTo(TestReports.Now)); // ten sam moment (offset może być UTC)
            Assert.That(loaded.UpdatedAt, Is.EqualTo(TestReports.Now.AddMinutes(5)));
        });
    }

    [Test]
    public async Task Data_SurvivesNewRepositoryInstance() // „restart aplikacji”
    {
        var report = TestReports.Create(title: "Zostaję w bazie");
        await new SqliteReportRepository(_paths).SaveAsync(report);
        SQLiteAsyncConnection.ResetPool();

        var reloaded = await new SqliteReportRepository(_paths).GetAllAsync();

        Assert.That(reloaded.Select(r => r.Id), Is.EqualTo(new[] { report.Id }));
    }

    [Test]
    public async Task Save_SameId_Twice_UpdatesInsteadOfDuplicating()
    {
        var repository = new SqliteReportRepository(_paths);
        var report = TestReports.Create(status: ReportStatus.New);
        await repository.SaveAsync(report);
        report.ChangeStatus(ReportStatus.InProgress, TestReports.Now.AddHours(1));

        await repository.SaveAsync(report);

        var all = await repository.GetAllAsync();
        Assert.That(all, Has.Count.EqualTo(1));
        Assert.That(all[0].Status, Is.EqualTo(ReportStatus.InProgress));
    }

    [Test]
    public async Task GetAll_SortsByNewestUpdate()
    {
        var repository = new SqliteReportRepository(_paths);
        await repository.SaveAsync(TestReports.Create(title: "Starsze", updatedAt: TestReports.Now.AddHours(-2)));
        await repository.SaveAsync(TestReports.Create(title: "Nowsze", updatedAt: TestReports.Now));

        var all = await repository.GetAllAsync();

        Assert.That(all.Select(r => r.Title.Value), Is.EqualTo(new[] { "Nowsze", "Starsze" }));
    }


    [Test]
    public async Task Delete_RemovesRow_AndMissingIdIsNoOp()
    {
        var repository = new SqliteReportRepository(_paths);
        var report = TestReports.Create();
        await repository.SaveAsync(report);

        await repository.DeleteAsync(report.Id);
        await repository.DeleteAsync(Guid.NewGuid());

        Assert.That(await repository.GetAsync(report.Id), Is.Null);
    }

    [Test]
    public async Task ParallelFirstQueries_OpenDatabaseOnce()
    {
        var repository = new SqliteReportRepository(_paths);
        var tasks = Enumerable.Range(0, 10).Select(_ => repository.GetAllAsync());
        await Task.WhenAll(tasks);
        Assert.That(File.Exists(Path.Combine(_dir, SqliteReportRepository.FileName)), Is.True);
    }

    [Test]
    public async Task NewDatabase_IsMigratedToLatestVersion()
    {
        await new SqliteReportRepository(_paths).GetAllAsync();

        var db = new SQLiteAsyncConnection(Path.Combine(_dir, SqliteReportRepository.FileName));
        Assert.That(await db.ExecuteScalarAsync<int>("PRAGMA user_version"), Is.EqualTo(SqliteMigrations.LatestVersion));
        await db.CloseAsync();
    }

    [Test]
    public async Task DeleteAll_RemovesAllReports() // wylogowanie
    {
        var repository = new SqliteReportRepository(_paths);
        await repository.SaveAsync(TestReports.Create());
        await repository.SaveAsync(TestReports.Create());

        await repository.DeleteAllAsync();

        Assert.That(await repository.GetAllAsync(), Is.Empty);
    }

    [Test]
    public async Task Replace_RemovesOldRow_AndSavesUnderNewId() // wysyłka: zgłoszenie dostało Id z serwera
    {
        var repository = new SqliteReportRepository(_paths);
        var report = TestReports.Create(syncState: SyncState.Pending);
        var localId = report.Id;
        await repository.SaveAsync(report);
        report.MarkOnServer(Guid.NewGuid());

        await repository.ReplaceAsync(localId, report);

        var all = await repository.GetAllAsync();
        Assert.That(all.Select(r => r.Id), Is.EqualTo(new[] { report.Id })); // jeden wiersz, pod nowym Id
        Assert.That(all[0].IsOnServer, Is.True);
    }

    [Test]
    public async Task IsOnServer_RoundTrips() // zmiana statusu zgłoszenia z serwera czeka w telefonie
    {
        var repository = new SqliteReportRepository(_paths);
        var report = TestReports.Create(syncState: SyncState.Synced); // zgłoszenie z serwera
        report.ChangeStatus(ReportStatus.InProgress, TestReports.Now);
        await repository.SaveAsync(report);

        var loaded = await repository.GetAsync(report.Id);

        Assert.That(loaded!.IsOnServer, Is.True);
        Assert.That(loaded.CanDelete, Is.False);
    }

    [Test]
    public async Task Version1Database_IsUpgraded_AndOldRowsAreNotOnServer()
    {
        // baza z wersji 1 (checkpoint SQLite): tabela bez kolumny IsOnServer
        var file = Path.Combine(_dir, SqliteReportRepository.FileName);
        var old = new SQLiteAsyncConnection(file);
        await old.ExecuteAsync("CREATE TABLE reports (Id varchar(36) PRIMARY KEY NOT NULL, Title varchar, Description varchar, " +
                               "Priority integer, Status integer, PhotoPath varchar, CreatedAt bigint, UpdatedAt bigint)");
        var id = Guid.NewGuid();
        await old.ExecuteAsync("INSERT INTO reports VALUES (?, 'Lokalne', NULL, 1, 0, NULL, ?, ?)", id.ToString(),
            TestReports.Now.UtcTicks, TestReports.Now.UtcTicks);
        await old.ExecuteAsync("PRAGMA user_version = 1");
        await old.CloseAsync();

        var loaded = await new SqliteReportRepository(_paths).GetAsync(id);

        Assert.That(loaded!.IsOnServer, Is.False);
        Assert.That(loaded.CanDelete, Is.True);
    }

    [Test]
    public async Task IdempotencyKey_RoundTrips() // ten sam klucz po „restarcie aplikacji”
    {
        var report = Report.CreateNew(Guid.NewGuid(), ReportTitle.Create("Awaria pompy").Value!, null, ReportPriority.High, TestReports.Now);
        await new SqliteReportRepository(_paths).SaveAsync(report);
        SQLiteAsyncConnection.ResetPool();

        var loaded = await new SqliteReportRepository(_paths).GetAsync(report.Id);

        Assert.That(loaded!.IdempotencyKey, Is.EqualTo(report.IdempotencyKey));
    }

    [Test]
    public async Task Version2Database_IsUpgraded_AndUnsentRowsGetIdempotencyKey()
    {
        // baza z wersji 2: tabela bez kolumny IdempotencyKey; jedno niewysłane i jedno z serwera (zmiana statusu)
        var file = Path.Combine(_dir, SqliteReportRepository.FileName);
        var old = new SQLiteAsyncConnection(file);
        await old.ExecuteAsync("CREATE TABLE reports (Id varchar(36) PRIMARY KEY NOT NULL, Title varchar, Description varchar, " +
                               "Priority integer, Status integer, PhotoPath varchar, CreatedAt bigint, UpdatedAt bigint, IsOnServer integer)");
        var unsent = Guid.NewGuid();
        var onServer = Guid.NewGuid();
        foreach (var (id, isOnServer) in new[] { (unsent, 0), (onServer, 1) })
            await old.ExecuteAsync("INSERT INTO reports VALUES (?, 'Lokalne', NULL, 1, 0, NULL, ?, ?, ?)", id.ToString(),
                TestReports.Now.UtcTicks, TestReports.Now.UtcTicks, isOnServer);
        await old.ExecuteAsync("PRAGMA user_version = 2");
        await old.CloseAsync();

        var repository = new SqliteReportRepository(_paths);
        var first = await repository.GetAsync(unsent);
        var again = await repository.GetAsync(unsent);
        var fromServer = await repository.GetAsync(onServer);

        Assert.That(first!.IdempotencyKey, Is.Not.Null.And.Not.EqualTo(Guid.Empty));
        Assert.That(again!.IdempotencyKey, Is.EqualTo(first.IdempotencyKey)); // klucz zapisany, nie losowany przy odczycie
        Assert.That(fromServer!.IdempotencyKey, Is.Null);
    }
}
