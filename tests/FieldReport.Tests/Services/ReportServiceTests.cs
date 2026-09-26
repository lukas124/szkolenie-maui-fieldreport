using FieldReport.Core.Data;
using FieldReport.Core.Data.Api;
using FieldReport.Core.Models;
using FieldReport.Core.Services;
using FieldReport.Core.Services.Sync;
using FieldReport.Core.ValueObjects;
using FieldReport.Tests.TestData;

namespace FieldReport.Tests.Services;

[TestFixture]
public class ReportServiceTests
{
    private Mock<IReportRepository> _repository = null!;
    private Mock<IReportsApiClient> _api = null!;
    private Mock<ISyncScheduler> _sync = null!;
    private Mock<IPhotoStorage> _photos = null!;
    private Mock<IConnectivityService> _connectivity = null!;
    private ReportService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _connectivity = new Mock<IConnectivityService>();
        _connectivity.SetupGet(c => c.IsOnline).Returns(true);
        _repository = new Mock<IReportRepository>();
        _repository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _api = new Mock<IReportsApiClient>();
        _api.Setup(a => a.GetReportsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(Result<IReadOnlyList<ReportDto>>.Success([]));
        _api.Setup(a => a.GetReportAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ReportDto>.Failure(Errors.NotFound));
        _sync = new Mock<ISyncScheduler>();
        _photos = new Mock<IPhotoStorage>();
        _service = new ReportService(_repository.Object, _api.Object, _connectivity.Object, Mock.Of<IClock>(c => c.Now == TestReports.Now.AddHours(1)),
            _sync.Object, _photos.Object);
    }

    private static ReportDto Dto(Report report) => ReportDtoMapper.ToDto(report);

    [Test]
    public async Task GetAll_MergesServerAndPending_FromNewestUpdate()
    {
        var onServer = TestReports.Create(title: "Z serwera", updatedAt: TestReports.Now.AddHours(-3));
        var pending = TestReports.Create(title: "Niewysłane", syncState: SyncState.Pending, updatedAt: TestReports.Now);
        _repository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync([pending]);
        _api.Setup(a => a.GetReportsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(Result<IReadOnlyList<ReportDto>>.Success([Dto(onServer)]));

        var list = await _service.GetAllAsync();

        Assert.That(list.IsOffline, Is.False);
        Assert.That(list.Reports.Select(r => r.Title.Value), Is.EqualTo(new[] { "Niewysłane", "Z serwera" }));
    }

    [Test]
    public async Task GetAll_ReportWithLocalChange_IsShownInPhoneVersion()
    {
        var onServer = TestReports.Create(status: ReportStatus.New);
        var changed = TestReports.Create(id: onServer.Id, status: ReportStatus.New, syncState: SyncState.Synced);
        changed.ChangeStatus(ReportStatus.InProgress, TestReports.Now);
        _repository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync([changed]);
        _api.Setup(a => a.GetReportsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(Result<IReadOnlyList<ReportDto>>.Success([Dto(onServer)]));

        var list = await _service.GetAllAsync();

        Assert.That(list.Reports.Single().Status, Is.EqualTo(ReportStatus.InProgress));
        Assert.That(list.Reports.Single().SyncState, Is.EqualTo(SyncState.Pending));
    }

    [Test]
    public async Task GetAll_SyncFinishedDuringRead_ShowsReportOnce() // wysyłka w tle skończyła się w trakcie odczytu listy
    {
        var pending = TestReports.Create(title: "Z kreatora", syncState: SyncState.Pending); // lokalne Id
        var onServer = TestReports.Create(title: "Z kreatora");                              // to samo, z Id nadanym przez serwer
        _repository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync([pending]);
        _api.Setup(a => a.GetReportsAsync(It.IsAny<CancellationToken>()))
            .Callback(() => _repository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync([])) // wysłane: znika z telefonu
            .ReturnsAsync(Result<IReadOnlyList<ReportDto>>.Success([Dto(onServer)]));

        var list = await _service.GetAllAsync();

        Assert.That(list.Reports.Select(r => r.Title.Value), Is.EqualTo(new[] { "Z kreatora" }));
    }

    [Test]
    public async Task GetAll_ServerUnavailable_ReturnsOnlyPending_AndIsOffline()
    {
        var pending = TestReports.Create(title: "Niewysłane", syncState: SyncState.Pending);
        _repository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync([pending]);
        _api.Setup(a => a.GetReportsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(Result<IReadOnlyList<ReportDto>>.Failure(Errors.Network));

        var list = await _service.GetAllAsync();

        Assert.That(list.IsOffline, Is.True);
        Assert.That(list.Reports.Select(r => r.Id), Is.EqualTo(new[] { pending.Id }));
    }

    [Test]
    public async Task Get_PendingOnPhone_IsReturnedWithoutCallingApi()
    {
        var pending = TestReports.Create(syncState: SyncState.Pending);
        _repository.Setup(r => r.GetAsync(pending.Id, It.IsAny<CancellationToken>())).ReturnsAsync(pending);

        var result = await _service.GetAsync(pending.Id);

        Assert.That(result.Value, Is.SameAs(pending));
        _api.Verify(a => a.GetReportAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task Get_NotOnPhone_IsDownloadedFromApi() // np. tapnięcie powiadomienia o nowym zgłoszeniu
    {
        var onServer = TestReports.Create(title: "Od dyspozytora");
        _api.Setup(a => a.GetReportAsync(onServer.Id, It.IsAny<CancellationToken>())).ReturnsAsync(Result<ReportDto>.Success(Dto(onServer)));

        var result = await _service.GetAsync(onServer.Id);

        Assert.That(result.Value!.Title.Value, Is.EqualTo("Od dyspozytora"));
        Assert.That(result.Value.IsOnServer, Is.True);
    }

    [Test]
    public async Task GetAll_Offline_ReturnsPendingWithoutCallingApi() // nie czekamy na timeout żądania
    {
        _connectivity.SetupGet(c => c.IsOnline).Returns(false);

        var list = await _service.GetAllAsync();

        Assert.That(list.IsOffline, Is.True);
        _api.Verify(a => a.GetReportsAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task Get_MissingEverywhere_FailsWithReportNotFound()
    {
        var result = await _service.GetAsync(Guid.NewGuid());

        Assert.That(result.Error, Is.EqualTo(Errors.ReportNotFound));
    }

    [Test]
    public async Task Delete_SentReportWithLocalChanges_FailsWithAlreadySynced()
    {
        var report = TestReports.Create(status: ReportStatus.InProgress, syncState: SyncState.Synced);
        report.ChangeStatus(ReportStatus.Done, TestReports.Now);
        _repository.Setup(r => r.GetAsync(report.Id, It.IsAny<CancellationToken>())).ReturnsAsync(report);

        var result = await _service.DeleteAsync(report.Id);

        Assert.That(result.Error, Is.EqualTo(Errors.ReportAlreadySynced));
        _repository.Verify(r => r.DeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task Delete_SyncedReport_FailsWithAlreadySynced_AndDoesNotDelete()
    {
        var report = TestReports.Create(syncState: SyncState.Synced);
        _repository.Setup(r => r.GetAsync(report.Id, It.IsAny<CancellationToken>())).ReturnsAsync(report);

        var result = await _service.DeleteAsync(report.Id);

        Assert.That(result.Error, Is.EqualTo(Errors.ReportAlreadySynced));
        _repository.Verify(r => r.DeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task Delete_PendingReport_DeletesReportAndPhoto()
    {
        var report = Report.CreateNew(Guid.NewGuid(), ReportTitle.Create("Ze zdjęciem").Value!, null, ReportPriority.Low,
            TestReports.Now, "/photos/a.jpg");
        _repository.Setup(r => r.GetAsync(report.Id, It.IsAny<CancellationToken>())).ReturnsAsync(report);

        var result = await _service.DeleteAsync(report.Id);

        Assert.That(result.IsSuccess, Is.True);
        _repository.Verify(r => r.DeleteAsync(report.Id, It.IsAny<CancellationToken>()), Times.Once);
        _photos.Verify(p => p.DeleteAsync("/photos/a.jpg", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task ChangeStatus_ServerReport_IsSavedOnPhoneAsPending_AndSyncRequested_EvenOffline()
    {
        var onServer = ReportDtoMapper.ToModel(Dto(TestReports.Create(status: ReportStatus.New))); // wyświetlane w szczegółach
        _connectivity.SetupGet(c => c.IsOnline).Returns(false);
        Report? saved = null;
        _repository.Setup(r => r.SaveAsync(It.IsAny<Report>(), It.IsAny<CancellationToken>()))
                   .Callback<Report, CancellationToken>((r, _) => saved = r);

        var result = await _service.ChangeStatusAsync(onServer, ReportStatus.InProgress);

        Assert.That(result.IsSuccess, Is.True);
        Assert.Multiple(() =>
        {
            Assert.That(saved!.Status, Is.EqualTo(ReportStatus.InProgress));
            Assert.That(saved.SyncState, Is.EqualTo(SyncState.Pending));
            Assert.That(saved.IsOnServer, Is.True);
        });
        _sync.Verify(s => s.RequestSync(), Times.Once);
    }

    [Test]
    public async Task ChangeStatus_ForbiddenTransition_ReturnsError_AndDoesNotSave()
    {
        var report = TestReports.Create(status: ReportStatus.Done);

        var result = await _service.ChangeStatusAsync(report, ReportStatus.New);

        Assert.That(result.Error, Is.EqualTo(Errors.InvalidStatusTransition));
        _repository.Verify(r => r.SaveAsync(It.IsAny<Report>(), It.IsAny<CancellationToken>()), Times.Never);
    }


    [Test]
    public async Task GetAll_EqualUpdateDates_HaveStableOrder()
    {
        var middle = TestReports.Create(title: "Utworzone 10:00", updatedAt: TestReports.Now);
        var a = new Report(Guid.Parse("00000000-0000-0000-0000-000000000002"), ReportTitle.Create("BBB").Value!, null, ReportPriority.Low,
            ReportStatus.New, SyncState.Synced, null, TestReports.Now.AddHours(-1), TestReports.Now, isOnServer: true);
        var b = new Report(Guid.Parse("00000000-0000-0000-0000-000000000001"), ReportTitle.Create("AAA").Value!, null, ReportPriority.Low,
            ReportStatus.New, SyncState.Synced, null, TestReports.Now.AddHours(-1), TestReports.Now, isOnServer: true);
        var newerCreation = new Report(Guid.NewGuid(), ReportTitle.Create("Nowsze utworzenie").Value!, null, ReportPriority.Low,
            ReportStatus.New, SyncState.Synced, null, TestReports.Now.AddMinutes(1), TestReports.Now, isOnServer: true);
        _repository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync([a, middle, b, newerCreation]);

        var first = (await _service.GetAllAsync()).Reports.Select(r => r.Title.Value).ToList();
        _repository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync([newerCreation, b, middle, a]);
        var second = (await _service.GetAllAsync()).Reports.Select(r => r.Title.Value).ToList();

        Assert.That(first, Is.EqualTo(new[] { "Nowsze utworzenie", "Utworzone 10:00", "AAA", "BBB" }));
        Assert.That(second, Is.EqualTo(first));
    }

    [Test]
    public async Task Delete_MissingReport_FailsWithNotFound()
    {
        var result = await _service.DeleteAsync(Guid.NewGuid());

        Assert.That(result.Error, Is.EqualTo(Errors.ReportNotFound));
    }
}

[TestFixture]
public class ReportServiceCreateTests
{
    private Mock<IReportRepository> _repository = null!;
    private Mock<ISyncScheduler> _sync = null!;
    private ReportService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _repository = new Mock<IReportRepository>();
        _sync = new Mock<ISyncScheduler>();
        _service = new ReportService(_repository.Object, Mock.Of<IReportsApiClient>(), Mock.Of<IConnectivityService>(),
            Mock.Of<IClock>(c => c.Now == TestReports.Now),
            _sync.Object, Mock.Of<IPhotoStorage>());
    }

    [Test]
    public async Task Create_SavesNewPendingReport_WithAppGeneratedId()
    {
        Report? saved = null;
        _repository.Setup(r => r.SaveAsync(It.IsAny<Report>(), It.IsAny<CancellationToken>()))
                   .Callback<Report, CancellationToken>((r, _) => saved = r);
        var draft = new NewReportDraft
        {
            Title = ReportTitle.Create("Awaria pompy").Value, Description = "  Buczy  ", Priority = ReportPriority.High, PhotoPath = "/p.jpg"
        };

        var result = await _service.CreateAsync(draft);

        Assert.That(result.IsSuccess, Is.True);
        Assert.Multiple(() =>
        {
            Assert.That(saved!.Id, Is.EqualTo(result.Value).And.Not.EqualTo(Guid.Empty));
            Assert.That(saved.Status, Is.EqualTo(ReportStatus.New));
            Assert.That(saved.SyncState, Is.EqualTo(SyncState.Pending));
            Assert.That(saved.Description, Is.EqualTo("Buczy"));
            Assert.That(saved.Priority, Is.EqualTo(ReportPriority.High));
            Assert.That(saved.PhotoPath, Is.EqualTo("/p.jpg"));
            Assert.That(saved.CreatedAt, Is.EqualTo(TestReports.Now));
        });
    }

    [Test]
    public async Task Create_RequestsBackgroundSync() // kreator nowego zgłoszenia: zapis w telefonie → wysyłka w tle
    {
        await _service.CreateAsync(new NewReportDraft { Title = ReportTitle.Create("Awaria").Value });
        _sync.Verify(s => s.RequestSync(), Times.Once);
    }

    [Test]
    public async Task Create_WithoutTitle_FailsWithoutSaving()
    {
        var result = await _service.CreateAsync(new NewReportDraft());

        Assert.That(result.Error, Is.EqualTo(Errors.InvalidTitle));
        _repository.Verify(r => r.SaveAsync(It.IsAny<Report>(), It.IsAny<CancellationToken>()), Times.Never);
        _sync.Verify(s => s.RequestSync(), Times.Never);
    }

    [Test]
    public async Task Create_TooLongDescription_Fails()
    {
        var draft = new NewReportDraft { Title = ReportTitle.Create("Awaria").Value, Description = new string('x', 501) };
        Assert.That((await _service.CreateAsync(draft)).Error, Is.EqualTo(Errors.InvalidDescription));
    }

    [Test]
    public async Task Create_BlankDescription_IsStoredAsNull()
    {
        Report? saved = null;
        _repository.Setup(r => r.SaveAsync(It.IsAny<Report>(), It.IsAny<CancellationToken>()))
                   .Callback<Report, CancellationToken>((r, _) => saved = r);
        await _service.CreateAsync(new NewReportDraft { Title = ReportTitle.Create("Awaria").Value, Description = "   " });
        Assert.That(saved!.Description, Is.Null);
    }
}
