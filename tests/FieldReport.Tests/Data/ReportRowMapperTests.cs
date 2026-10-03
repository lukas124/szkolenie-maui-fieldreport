using FieldReport.Core.Data;
using FieldReport.Core.Models;
using FieldReport.Core.ValueObjects;
using FieldReport.Tests.TestData;

namespace FieldReport.Tests.Data;

[TestFixture]
public class ReportRowMapperTests
{
    [TestCase(ReportStatus.New, ReportPriority.Low, false)]
    [TestCase(ReportStatus.InProgress, ReportPriority.Normal, true)]
    [TestCase(ReportStatus.Done, ReportPriority.High, true)]
    public void RoundTrip_KeepsEnumsAndIsOnServer(ReportStatus status, ReportPriority priority, bool isOnServer)
    {
        var report = new Report(Guid.NewGuid(), FieldReport.Core.ValueObjects.ReportTitle.Create("Awaria").Value!, null, priority,
            status, SyncState.Pending, null, TestReports.Now, TestReports.Now, isOnServer);

        var copy = ReportRowMapper.ToModel(ReportRowMapper.ToRow(report));

        Assert.That((copy.Status, copy.Priority, copy.IsOnServer, copy.Title.Value), Is.EqualTo((status, priority, isOnServer, "Awaria")));
    }

    [Test]
    public void RoundTrip_KeepsIdempotencyKey() // ten sam klucz przy każdej próbie wysyłki (także po restarcie)
    {
        var report = Report.CreateNew(Guid.NewGuid(), ReportTitle.Create("Awaria").Value!, null, ReportPriority.Low, TestReports.Now);

        Assert.That(ReportRowMapper.ToModel(ReportRowMapper.ToRow(report)).IdempotencyKey, Is.EqualTo(report.IdempotencyKey));
    }

    [Test]
    public void ReportFromDatabase_AlwaysWaitsToBeSent() // w telefonie są tylko niewysłane
    {
        var copy = ReportRowMapper.ToModel(ReportRowMapper.ToRow(TestReports.Create(syncState: SyncState.Synced)));

        Assert.That(copy.SyncState, Is.EqualTo(SyncState.Pending));
    }

    [Test]
    public void CorruptedTitleInDatabase_Throws() =>
        Assert.That(() => ReportRowMapper.ToModel(new ReportRow { Id = Guid.NewGuid(), Title = "x" }), Throws.InstanceOf<InvalidDataException>());
}
