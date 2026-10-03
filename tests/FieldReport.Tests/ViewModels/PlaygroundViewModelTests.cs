using FieldReport.Core.Data;
using FieldReport.Core.Navigation;
using FieldReport.Core.Services;
using FieldReport.Core.ViewModels.Playground;
using FieldReport.Tests.TestData;

namespace FieldReport.Tests.ViewModels;

[TestFixture]
public class PlaygroundViewModelTests
{
    private InMemoryReportRepository _repository = null!;
    private PlaygroundViewModel _vm = null!;

    [SetUp]
    public void SetUp()
    {
        var clock = Mock.Of<IClock>(c => c.Now == TestReports.Now);
        _repository = new InMemoryReportRepository(clock);
        _vm = new PlaygroundViewModel(Mock.Of<INavigationService>(), new CounterViewModel(), _repository, clock);
    }

    [Test]
    public async Task ClearReports_EmptiesRepository()
    {
        await _vm.ClearReportsCommand.ExecuteAsync(null);

        Assert.That(await _repository.GetAllAsync(), Is.Empty);
    }

    [Test]
    public async Task RestoreSamples_AfterClear_BringsBackSixSamples()
    {
        await _vm.ClearReportsCommand.ExecuteAsync(null);

        await _vm.RestoreSamplesCommand.ExecuteAsync(null);

        Assert.That(await _repository.GetAllAsync(), Has.Count.EqualTo(6));
    }
}
