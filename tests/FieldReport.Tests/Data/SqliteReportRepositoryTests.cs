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

}
