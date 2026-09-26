using FieldReport.Core.Data;
using FieldReport.Core.Services;
using FieldReport.Tests.TestData;

namespace FieldReport.Tests.Data;

[TestFixture]
public class SampleReportsTests
{
    private InMemoryReportRepository _repository = null!;

    [SetUp]
    public void SetUp() => _repository = new InMemoryReportRepository(Mock.Of<IClock>(c => c.Now == TestReports.Now));

    [Test]
    public async Task RemoveAll_DeletesEveryReport_AlsoSynced()
    {
        await SampleReports.RemoveAllAsync(_repository);

        Assert.That(await _repository.GetAllAsync(), Is.Empty);
    }

    [Test]
    public async Task Restore_ReplacesReportsWithSixSamples()
    {
        await SampleReports.RemoveAllAsync(_repository);
        await _repository.SaveAsync(TestReports.Create()); // zgłoszenie spoza danych przykładowych

        await SampleReports.RestoreAsync(_repository, TestReports.Now);

        var all = await _repository.GetAllAsync();
        Assert.That(all.Select(r => r.Id), Is.EquivalentTo(SampleReports.Create(TestReports.Now).Select(r => r.Id)));
    }
}
