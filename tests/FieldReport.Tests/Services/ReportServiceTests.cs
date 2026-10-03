using FieldReport.Core.Data;
using FieldReport.Core.Models;
using FieldReport.Core.Services;
using FieldReport.Core.ValueObjects;
using FieldReport.Tests.TestData;

namespace FieldReport.Tests.Services;

[TestFixture]
public class ReportServiceTests
{
    private Mock<IReportRepository> _repository = null!;
    private ReportService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _repository = new Mock<IReportRepository>();
        _service = new ReportService(_repository.Object, Mock.Of<IClock>(c => c.Now == TestReports.Now.AddHours(1)));
    }

    [Test]
    public async Task GetAll_ReturnsReportsFromNewestUpdate()
    {
        var older = TestReports.Create(title: "Starsze", updatedAt: TestReports.Now.AddHours(-3));
        var newer = TestReports.Create(title: "Nowsze", updatedAt: TestReports.Now);
        _repository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync([older, newer]);

        var result = await _service.GetAllAsync();

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value!.Select(r => r.Title.Value), Is.EqualTo(new[] { "Nowsze", "Starsze" }));
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
    public async Task Delete_PendingReport_Deletes()
    {
        var report = TestReports.Create(syncState: SyncState.Pending);
        _repository.Setup(r => r.GetAsync(report.Id, It.IsAny<CancellationToken>())).ReturnsAsync(report);

        var result = await _service.DeleteAsync(report.Id);

        Assert.That(result.IsSuccess, Is.True);
        _repository.Verify(r => r.DeleteAsync(report.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task ChangeStatus_AllowedTransition_SavesReport()
    {
        var report = TestReports.Create(status: ReportStatus.New, syncState: SyncState.Synced);
        _repository.Setup(r => r.GetAsync(report.Id, It.IsAny<CancellationToken>())).ReturnsAsync(report);

        var result = await _service.ChangeStatusAsync(report.Id, ReportStatus.InProgress);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value!.Status, Is.EqualTo(ReportStatus.InProgress));
        _repository.Verify(r => r.SaveAsync(report, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task ChangeStatus_ForbiddenTransition_ReturnsError_AndDoesNotSave()
    {
        var report = TestReports.Create(status: ReportStatus.Done);
        _repository.Setup(r => r.GetAsync(report.Id, It.IsAny<CancellationToken>())).ReturnsAsync(report);

        var result = await _service.ChangeStatusAsync(report.Id, ReportStatus.New);

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

        var first = (await _service.GetAllAsync()).Value!.Select(r => r.Title.Value).ToList();
        _repository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync([newerCreation, b, middle, a]);
        var second = (await _service.GetAllAsync()).Value!.Select(r => r.Title.Value).ToList();

        Assert.That(first, Is.EqualTo(new[] { "Nowsze utworzenie", "Utworzone 10:00", "AAA", "BBB" }));
        Assert.That(second, Is.EqualTo(first));
    }

    [Test]
    public async Task ChangeStatus_MissingReport_FailsWithNotFound()
    {
        var result = await _service.ChangeStatusAsync(Guid.NewGuid(), ReportStatus.InProgress);

        Assert.That(result.Error, Is.EqualTo(Errors.ReportNotFound));
        _repository.Verify(r => r.SaveAsync(It.IsAny<Report>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task Delete_MissingReport_FailsWithNotFound()
    {
        var result = await _service.DeleteAsync(Guid.NewGuid());

        Assert.That(result.Error, Is.EqualTo(Errors.ReportNotFound));
    }
}
