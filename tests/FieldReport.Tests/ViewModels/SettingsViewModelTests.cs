using FieldReport.Core.Models;
using FieldReport.Core.Navigation;
using FieldReport.Core.Presentation;
using FieldReport.Core.Services;
using FieldReport.Core.Services.Push;
using FieldReport.Core.ViewModels;
using FieldReport.Core.ViewModels.Playground;
using FieldReport.Tests.TestData;

namespace FieldReport.Tests.ViewModels;

[TestFixture]
public class SettingsViewModelTests
{
    private Mock<INavigationService> _navigation = null!;
    private Mock<IAccountService> _account = null!;
    private Mock<INotificationSettingsService> _notifications = null!;
    private Mock<IUserFeedback> _feedback = null!;
    private Mock<ILocalizer> _localizer = null!;
    private SettingsViewModel _vm = null!;

    [SetUp]
    public void SetUp()
    {
        _navigation = new Mock<INavigationService>();
        _account = new Mock<IAccountService>();
        _notifications = new Mock<INotificationSettingsService>();
        _feedback = new Mock<IUserFeedback>();
        var env = Mock.Of<IAppEnvironment>(e => e.EnvironmentName == "DEV" && e.VersionText == "1.0.0 (42)" && e.IsDebug);
        _localizer = new Mock<ILocalizer>();
        _localizer.Setup(l => l.CurrentCulture).Returns(System.Globalization.CultureInfo.GetCultureInfo("pl-PL"));
        _vm = new SettingsViewModel(_navigation.Object, _account.Object, env, _notifications.Object, _localizer.Object,
            _feedback.Object);
    }

    [Test]
    public async Task ShowsTechnician_EnvironmentVersion_AndNotificationState()
    {
        _account.Setup(a => a.GetSessionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Session("jan@firma.pl", "Jan Kowalski", "t", "r"));
        _notifications.Setup(n => n.GetStatusAsync()).ReturnsAsync(PushPermissionStatus.Granted);

        await _vm.OnNavigatedToAsync(null);

        Assert.Multiple(() =>
        {
            Assert.That(_vm.TechnicianName, Is.EqualTo("Jan Kowalski"));
            Assert.That(_vm.EnvironmentName, Is.EqualTo("DEV"));
            Assert.That(_vm.VersionText, Is.EqualTo("1.0.0 (42)"));
            Assert.That(_vm.AreNotificationsEnabled, Is.True);
            Assert.That(_vm.IsDebug, Is.True);
        });
    }

    [Test]
    public async Task OnResumed_RefreshesNotificationState() // powrót z ustawień systemu = Window.Resumed
    {
        _notifications.Setup(n => n.GetStatusAsync()).ReturnsAsync(PushPermissionStatus.Granted);
        await _vm.OnResumedAsync();
        Assert.That(_vm.AreNotificationsEnabled, Is.True);
    }

    [Test]
    public async Task EnableNotifications_Success_UpdatesState()
    {
        _notifications.Setup(n => n.EnableAsync(It.IsAny<CancellationToken>()))
                      .ReturnsAsync(Result<PushPermissionStatus>.Success(PushPermissionStatus.Granted));

        await _vm.EnableNotificationsCommand.ExecuteAsync(null);

        Assert.That(_vm.AreNotificationsEnabled, Is.True);
    }

    [Test]
    public async Task EnableNotifications_RegistrationFails_ShowsError()
    {
        _notifications.Setup(n => n.EnableAsync(It.IsAny<CancellationToken>()))
                      .ReturnsAsync(Result<PushPermissionStatus>.Failure(Errors.Network));

        await _vm.EnableNotificationsCommand.ExecuteAsync(null);

        _feedback.Verify(f => f.ShowErrorAsync(Errors.Network), Times.Once);
    }

    [Test]
    public async Task CopyToken_CopiesToClipboard_AndConfirms()
    {
        _notifications.Setup(n => n.CopyTokenAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);

        await _vm.CopyPushTokenCommand.ExecuteAsync(null);

        _feedback.Verify(f => f.ShowInfoAsync("Settings_TokenCopiedTitle", "Settings_TokenCopiedMessage"), Times.Once);
    }

    [Test]
    public async Task CopyToken_NoToken_ShowsError() // iOS bez APNs, emulator bez Google Play
    {
        _notifications.Setup(n => n.CopyTokenAsync(It.IsAny<CancellationToken>())).ReturnsAsync(false);

        await _vm.CopyPushTokenCommand.ExecuteAsync(null);

        _feedback.Verify(f => f.ShowErrorAsync(Errors.PushTokenUnavailable), Times.Once);
    }

    [Test]
    public async Task SignOut_Confirmed_SignsOut_AndResetsToLogin()
    {
        _feedback.Setup(f => f.ConfirmAsync("Settings_SignOutTitle", "Settings_SignOutMessage")).ReturnsAsync(true);

        await _vm.SignOutCommand.ExecuteAsync(null);

        _account.Verify(a => a.SignOutAsync(It.IsAny<CancellationToken>()), Times.Once);
        _navigation.Verify(n => n.ResetToAsync<LoginViewModel>(), Times.Once);
    }

    [Test]
    public async Task SignOut_WithUnsentReports_WarnsTheyWillBeDeleted()
    {
        _account.Setup(a => a.CountPendingAsync(It.IsAny<CancellationToken>())).ReturnsAsync(2);

        await _vm.SignOutCommand.ExecuteAsync(null);

        _feedback.Verify(f => f.ConfirmAsync("Settings_SignOutTitle", "Settings_SignOutPendingMessage"), Times.Once);
        _account.Verify(a => a.SignOutAsync(It.IsAny<CancellationToken>()), Times.Never); // nie potwierdzono
    }

    [Test]
    public async Task SignOut_NotConfirmed_DoesNothing()
    {
        await _vm.SignOutCommand.ExecuteAsync(null);
        _account.Verify(a => a.SignOutAsync(It.IsAny<CancellationToken>()), Times.Never);
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
