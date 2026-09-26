using FieldReport.Core.Models;
using FieldReport.Core.Presentation;
using FieldReport.Tests.TestData;
using FieldReport.Core.ValueObjects;

namespace FieldReport.Tests.Presentation;

[TestFixture]
public class ReportItemFactoryTests
{
    private ReportItemFactory _factory = null!;

    [SetUp]
    public void SetUp()
    {
        var formatter = new Mock<IReportFormatter>();
        formatter.Setup(f => f.FormatDate(It.IsAny<DateTimeOffset>())).Returns("26.09.2026 10:00");
        formatter.Setup(f => f.FormatStatus(It.IsAny<ReportStatus>())).Returns((ReportStatus s) => $"status:{s}");
        formatter.Setup(f => f.FormatPriority(It.IsAny<ReportPriority>())).Returns((ReportPriority p) => $"priority:{p}");
        _factory = new ReportItemFactory(formatter.Object);
    }

    [Test]
    public void Create_NotSentReport_IsPendingSync()
    {
        var item = _factory.Create(TestReports.Create(syncState: SyncState.Pending));

        Assert.That(item.IsPendingSync, Is.True);
    }

    [Test]
    public void Create_SyncedReport_IsNotPendingSync()
    {
        var item = _factory.Create(TestReports.Create(syncState: SyncState.Synced));

        Assert.That(item.IsPendingSync, Is.False);
    }

    [Test]
    public void Create_UsesFormatterForTexts()
    {
        var item = _factory.Create(TestReports.Create(title: "Wyciek z instalacji", status: ReportStatus.InProgress, priority: ReportPriority.High));

        Assert.Multiple(() =>
        {
            Assert.That(item.Title, Is.EqualTo("Wyciek z instalacji"));
            Assert.That(item.CreatedAtText, Is.EqualTo("26.09.2026 10:00"));
            Assert.That(item.StatusText, Is.EqualTo("status:InProgress"));
            Assert.That(item.PriorityText, Is.EqualTo("priority:High"));
            Assert.That(item.Priority, Is.EqualTo(ReportPriority.High));
        });
    }

    [Test]
    public void Create_ShowsCreationDate_NotLastUpdate() // lista zgłoszeń: karta pokazuje datę UTWORZENIA
    {
        var formatter = new Mock<IReportFormatter>();
        formatter.Setup(f => f.FormatDate(It.IsAny<DateTimeOffset>())).Returns((DateTimeOffset d) => d.ToString("yyyy-MM-dd HH:mm"));
        var created = new DateTimeOffset(2026, 9, 20, 8, 0, 0, TimeSpan.Zero);
        var report = new Report(Guid.NewGuid(), ReportTitle.Create("Awaria").Value!, null, ReportPriority.Low, ReportStatus.InProgress,
            SyncState.Synced, null, created, created.AddDays(5));

        var item = new ReportItemFactory(formatter.Object).Create(report);

        Assert.That(item.CreatedAtText, Is.EqualTo("2026-09-20 08:00"));
    }
}
