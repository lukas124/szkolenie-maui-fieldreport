using FieldReport.Core.Models;
using FieldReport.Core.ValueObjects;
using FieldReport.Tests.TestData;

namespace FieldReport.Tests.Models;

[TestFixture]
public class ReportTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 26, 10, 0, 0, TimeSpan.FromHours(2));

    [Test]
    public void CreateNew_StartsAsNew_AndWaitsForSync()
    {
        var report = Report.CreateNew(Guid.NewGuid(), ReportTitle.Create("Awaria pompy").Value!, null, ReportPriority.High, Now);

        Assert.Multiple(() =>
        {
            Assert.That(report.Status, Is.EqualTo(ReportStatus.New));
            Assert.That(report.SyncState, Is.EqualTo(SyncState.Pending));
            Assert.That(report.CreatedAt, Is.EqualTo(Now));
            Assert.That(report.UpdatedAt, Is.EqualTo(Now));
        });
    }

    // ----- reguła przejść statusu -----

    private static Report WithStatus(ReportStatus status) =>
        new(Guid.NewGuid(), ReportTitle.Create("Awaria pompy").Value!, null, ReportPriority.Normal,
            status, SyncState.Synced, null, Now, Now, isOnServer: true);

    [Test]
    public void ChangeStatus_FromDone_ReturnsError()
    {
        var report = WithStatus(ReportStatus.Done);

        var result = report.ChangeStatus(ReportStatus.New, Now.AddHours(1));

        Assert.That(result.Error, Is.EqualTo(Errors.InvalidStatusTransition));
        Assert.That(report.Status, Is.EqualTo(ReportStatus.Done));
    }

    [TestCase(ReportStatus.New, ReportStatus.InProgress)]
    [TestCase(ReportStatus.InProgress, ReportStatus.Done)]
    public void ChangeStatus_AllowedTransition_UpdatesStatus_AndMarksForSync(ReportStatus from, ReportStatus to)
    {
        var report = WithStatus(from);
        var later = Now.AddHours(1);

        var result = report.ChangeStatus(to, later);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(report.Status, Is.EqualTo(to));
            Assert.That(report.UpdatedAt, Is.EqualTo(later));
            Assert.That(report.SyncState, Is.EqualTo(SyncState.Pending)); // zmiana offline → do wysłania
        });
    }

    [TestCase(ReportStatus.InProgress, ReportStatus.New)]   // wstecz
    [TestCase(ReportStatus.Done, ReportStatus.InProgress)]  // wstecz
    [TestCase(ReportStatus.Done, ReportStatus.New)]
    [TestCase(ReportStatus.New, ReportStatus.New)]           // ten sam status
    [TestCase(ReportStatus.InProgress, ReportStatus.InProgress)]
    public void ChangeStatus_BackwardsOrSame_FailsWithCode_AndKeepsState(ReportStatus from, ReportStatus to)
    {
        var report = TestReports.Create(status: from, syncState: SyncState.Synced);

        var result = report.ChangeStatus(to, TestReports.Now.AddHours(1));

        Assert.That(result.Error, Is.EqualTo(Errors.InvalidStatusTransition));
        Assert.That((report.Status, report.SyncState, report.UpdatedAt), Is.EqualTo((from, SyncState.Synced, TestReports.Now)));
    }

    [Test]
    public void ChangeStatus_SkippingInProgress_ReturnsError()
    {
        var result = WithStatus(ReportStatus.New).ChangeStatus(ReportStatus.Done, Now);

        Assert.That(result.IsSuccess, Is.False);
    }

    [TestCase(ReportStatus.New, new[] { ReportStatus.InProgress })]
    [TestCase(ReportStatus.InProgress, new[] { ReportStatus.Done })]
    [TestCase(ReportStatus.Done, new ReportStatus[0])]
    public void AllowedNextStatuses_FollowTheWorkflow(ReportStatus status, ReportStatus[] expected) =>
        Assert.That(WithStatus(status).AllowedNextStatuses, Is.EqualTo(expected));

    // ----- wysłane zgłoszenie ze zmianą do wysłania nadal nie może być usunięte -----

    [Test]
    public void SyncedReport_AfterStatusChange_IsPending_ButCannotBeDeleted()
    {
        var report = TestReports.Create(status: ReportStatus.InProgress, syncState: SyncState.Synced);

        report.ChangeStatus(ReportStatus.Done, TestReports.Now.AddHours(1));

        Assert.That(report.SyncState, Is.EqualTo(SyncState.Pending));
        Assert.That(report.IsOnServer, Is.True);
        Assert.That(report.CanDelete, Is.False);
    }

    [Test]
    public void NewLocalReport_CanBeDeleted() =>
        Assert.That(Report.CreateNew(Guid.NewGuid(), ReportTitle.Create("Awaria").Value!, null, ReportPriority.Low, TestReports.Now).CanDelete, Is.True);
}
