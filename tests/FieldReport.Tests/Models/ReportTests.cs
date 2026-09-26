using FieldReport.Core.Models;
using FieldReport.Core.ValueObjects;

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
}
