using FieldReport.Core.Models;
using FieldReport.Core.Navigation;
using FieldReport.Core.Presentation;
using FieldReport.Core.Services;
using FieldReport.Core.ViewModels;
using FieldReport.Core.ViewModels.Playground;
using FieldReport.Tests.TestData;

namespace FieldReport.Tests.ViewModels;

[TestFixture]
public class SettingsViewModelTests
{
    private Mock<INavigationService> _navigation = null!;
    private Mock<IAuthService> _auth = null!;
    private Mock<IUserFeedback> _feedback = null!;
    private Mock<ILocalizer> _localizer = null!;
    private SettingsViewModel _vm = null!;

    [SetUp]
    public void SetUp()
    {
        _navigation = new Mock<INavigationService>();
        _auth = new Mock<IAuthService>();
        _feedback = new Mock<IUserFeedback>();
        var env = Mock.Of<IAppEnvironment>(e => e.EnvironmentName == "DEV" && e.VersionText == "1.0.0 (42)");
        _localizer = new Mock<ILocalizer>();
        _localizer.Setup(l => l.CurrentCulture).Returns(System.Globalization.CultureInfo.GetCultureInfo("pl-PL"));
        _vm = new SettingsViewModel(_navigation.Object, _auth.Object, env, _localizer.Object, _feedback.Object);
    }

    [Test]
    public async Task ShowsTechnician_EnvironmentAndVersion()
    {
        _auth.Setup(a => a.GetSessionAsync(It.IsAny<CancellationToken>()))
             .ReturnsAsync(new Session("jan@firma.pl", "Jan Kowalski", "t", "r"));

        await _vm.OnNavigatedToAsync(null);

        Assert.Multiple(() =>
        {
            Assert.That(_vm.TechnicianName, Is.EqualTo("Jan Kowalski"));
            Assert.That(_vm.EnvironmentName, Is.EqualTo("DEV"));
            Assert.That(_vm.VersionText, Is.EqualTo("1.0.0 (42)"));
        });
    }

    [Test]
    public async Task SignOut_Confirmed_ClearsSession_AndResetsToLogin()
    {
        _feedback.Setup(f => f.ConfirmAsync("Settings_SignOutTitle", "Settings_SignOutMessage")).ReturnsAsync(true);

        await _vm.SignOutCommand.ExecuteAsync(null);

        _auth.Verify(a => a.SignOutAsync(It.IsAny<CancellationToken>()), Times.Once);
        _navigation.Verify(n => n.ResetToAsync<LoginViewModel>(), Times.Once);
    }

    [Test]
    public async Task SignOut_NotConfirmed_DoesNothing()
    {
        await _vm.SignOutCommand.ExecuteAsync(null);
        _auth.Verify(a => a.SignOutAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task Language_CurrentCultureIsSelected_AndChoosingEnglishSwitchesLanguage()
    {
        await _vm.OnNavigatedToAsync(null);
        Assert.That(_vm.SelectedLanguage!.Code, Is.EqualTo("pl-PL"));

        _vm.SelectedLanguage = _vm.Languages.Single(l => l.Code == "en");

        _localizer.Verify(l => l.SetLocale(It.Is<System.Globalization.CultureInfo>(c => c.Name == "en")), Times.Once);
    }

    [Test]
    public async Task OpenPlayground_NavigatesToPlayground()
    {
        await _vm.OpenPlaygroundCommand.ExecuteAsync(null);

        _navigation.Verify(n => n.NavigateToAsync<PlaygroundViewModel>(null), Times.Once);
    }
}
