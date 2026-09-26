using FieldReport.Core.Models;
using FieldReport.Core.Presentation;
using FieldReport.Tests.TestData;

namespace FieldReport.Tests.Presentation;

[TestFixture]
public class ReportDetailsFactoryTests
{
    private ReportDetailsFactory _factory = null!;

    [SetUp]
    public void SetUp()
    {
        var localizer = new KeyLocalizer(); // tekst = klucz zasobu
        _factory = new ReportDetailsFactory(new ReportFormatter(localizer), localizer);
    }

    [Test]
    public void Create_NewReport_OffersStart()
    {
        var details = _factory.Create(TestReports.Create(status: ReportStatus.New));

        Assert.That(details.Actions, Is.EqualTo(new[] { new StatusAction(ReportStatus.InProgress, "StatusAction_InProgress") }));
    }

    [Test]
    public void Create_InProgressReport_OffersFinish()
    {
        var details = _factory.Create(TestReports.Create(status: ReportStatus.InProgress));

        Assert.That(details.Actions, Is.EqualTo(new[] { new StatusAction(ReportStatus.Done, "StatusAction_Done") }));
    }

    [Test]
    public void Create_DoneReport_HasNoActions()
    {
        var details = _factory.Create(TestReports.Create(status: ReportStatus.Done));

        Assert.That(details.Actions, Is.Empty);
    }

    [Test]
    public void Create_PendingReport_ShowsSyncStateText()
    {
        var details = _factory.Create(TestReports.Create(syncState: SyncState.Pending));

        Assert.That(details.IsPendingSync, Is.True);
        Assert.That(details.SyncStateText, Is.EqualTo("SyncState_Pending"));
    }
}
