using FieldReport.Core.Data;
using FieldReport.Core.Services;
using FieldReport.Tests.TestData;

namespace FieldReport.Tests.Data;

[TestFixture]
public class InMemoryReportRepositoryTests
{
    private InMemoryReportRepository _repository = null!;

    [SetUp]
    public void SetUp() => _repository = new InMemoryReportRepository(Mock.Of<IClock>(c => c.Now == TestReports.Now));

    [Test]
    public async Task StartsWithSampleReports()
    {
        var all = await _repository.GetAllAsync();

        Assert.That(all, Has.Count.EqualTo(6));
        Assert.That(all.Select(r => r.Id).Distinct().Count(), Is.EqualTo(6));
    }

    [Test]
    public async Task Save_ThenGet_ReturnsSameReport_AndDeleteRemovesIt()
    {
        var report = TestReports.Create();

        await _repository.SaveAsync(report);
        Assert.That(await _repository.GetAsync(report.Id), Is.SameAs(report));

        await _repository.DeleteAsync(report.Id);
        Assert.That(await _repository.GetAsync(report.Id), Is.Null);
    }
}
