using FieldReport.Core.Navigation;
using FieldReport.Core.ViewModels;
using FieldReport.Core.ViewModels.Playground;

namespace FieldReport.Tests.ViewModels;

[TestFixture]
public class SettingsViewModelTests
{
    private Mock<INavigationService> _navigation = null!;
    private SettingsViewModel _vm = null!;

    [SetUp]
    public void SetUp()
    {
        _navigation = new Mock<INavigationService>();
        _vm = new SettingsViewModel(_navigation.Object);
    }

    [Test]
    public async Task OpenPlayground_NavigatesToPlayground()
    {
        await _vm.OpenPlaygroundCommand.ExecuteAsync(null);

        _navigation.Verify(n => n.NavigateToAsync<PlaygroundViewModel>(null), Times.Once);
    }
}
