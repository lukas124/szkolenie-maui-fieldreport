using CommunityToolkit.Mvvm.Messaging;
using FieldReport.Core.Models;
using FieldReport.Core.Navigation;
using FieldReport.Core.Presentation;
using FieldReport.Core.Services;
using FieldReport.Core.Services.Push;
using FieldReport.Core.Services.Sync;
using FieldReport.Core.Data;
using FieldReport.Core.ViewModels;
using FieldReport.Tests.TestData;

namespace FieldReport.Tests.Services.Push;

[TestFixture]
public class NotificationPrimingViewModelTests
{
    private Mock<INavigationService> _navigation = null!;
    private Mock<IPushPermissionService> _permission = null!;
    private Mock<IDeviceRegistrationService> _devices = null!;
    private Mock<IUserFeedback> _feedback = null!;
    private NotificationPrimingViewModel _vm = null!;

    [SetUp]
    public void SetUp()
    {
        _navigation = new Mock<INavigationService>();
        _permission = new Mock<IPushPermissionService>();
        _devices = new Mock<IDeviceRegistrationService>();
        _feedback = new Mock<IUserFeedback>();
        _vm = new NotificationPrimingViewModel(_navigation.Object, _permission.Object, _devices.Object, _feedback.Object);
    }

    [Test]
    public async Task Enable_Granted_RegistersDevice_AndCloses()
    {
        _permission.Setup(p => p.RequestAsync()).ReturnsAsync(PushPermissionStatus.Granted);
        _devices.Setup(d => d.RegisterAsync(It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success());

        await _vm.EnableCommand.ExecuteAsync(null);

        _devices.Verify(d => d.RegisterAsync(It.IsAny<CancellationToken>()), Times.Once);
        _navigation.Verify(n => n.GoBackAsync(), Times.Once);
        _feedback.Verify(f => f.ShowErrorAsync(It.IsAny<Error>()), Times.Never);
    }

    [Test]
    public async Task Enable_Denied_DoesNotRegister_ButCloses()
    {
        _permission.Setup(p => p.RequestAsync()).ReturnsAsync(PushPermissionStatus.DeniedPermanently);

        await _vm.EnableCommand.ExecuteAsync(null);

        _devices.Verify(d => d.RegisterAsync(It.IsAny<CancellationToken>()), Times.Never);
        _navigation.Verify(n => n.GoBackAsync(), Times.Once);
    }

    [Test]
    public async Task Enable_RegistrationFails_ShowsError_AndStillCloses()
    {
        _permission.Setup(p => p.RequestAsync()).ReturnsAsync(PushPermissionStatus.Granted);
        _devices.Setup(d => d.RegisterAsync(It.IsAny<CancellationToken>())).ReturnsAsync(Result.Failure(Errors.Network));

        await _vm.EnableCommand.ExecuteAsync(null);

        _feedback.Verify(f => f.ShowErrorAsync(Errors.Network), Times.Once);
        _navigation.Verify(n => n.GoBackAsync(), Times.Once);
    }

    [Test]
    public async Task NotNow_ClosesWithoutAsking()
    {
        await _vm.NotNowCommand.ExecuteAsync(null);

        _permission.Verify(p => p.RequestAsync(), Times.Never);
        _navigation.Verify(n => n.GoBackAsync(), Times.Once);
    }
}

[TestFixture]
public class NotificationOnboardingTests
{
    [Test]
    public async Task FirstTime_ShowsPriming_OnlyOnce()
    {
        var preferences = new Mock<IAppPreferences>();
        preferences.SetupProperty(p => p.NotificationPrimingShown, false);
        var devices = new Mock<IDeviceRegistrationService>();
        var onboarding = new NotificationOnboarding(preferences.Object, devices.Object);

        Assert.That(await onboarding.AfterSignInAsync(), Is.True);
        Assert.That(await onboarding.AfterSignInAsync(), Is.False);
        devices.Verify(d => d.RegisterIfPermittedAsync(It.IsAny<CancellationToken>()), Times.Once); // przy drugim logowaniu
    }
}

[TestFixture]
public class NotificationSettingsServiceTests
{
    private Mock<IPushPermissionService> _permission = null!;
    private Mock<IPermissionService> _permissions = null!;
    private Mock<IDeviceRegistrationService> _devices = null!;
    private NotificationSettingsService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _permission = new Mock<IPushPermissionService>();
        _permissions = new Mock<IPermissionService>();
        _devices = new Mock<IDeviceRegistrationService>();
        _devices.Setup(d => d.RegisterAsync(It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success());
        _service = new NotificationSettingsService(_permission.Object, _permissions.Object, _devices.Object, Mock.Of<IPushTokenProvider>(),
            Mock.Of<IClipboardService>());
    }

    [Test]
    public async Task Enable_DeniedPermanently_OpensSystemSettings_WithoutAsking()
    {
        _permission.Setup(p => p.CheckAsync()).ReturnsAsync(PushPermissionStatus.DeniedPermanently);

        var result = await _service.EnableAsync();

        _permissions.Verify(p => p.OpenAppSettings(), Times.Once);
        _permission.Verify(p => p.RequestAsync(), Times.Never);
        Assert.That(result.Value, Is.EqualTo(PushPermissionStatus.DeniedPermanently));
    }

    [Test]
    public async Task Enable_NotDetermined_Asks_AndRegistersWhenGranted()
    {
        _permission.Setup(p => p.CheckAsync()).ReturnsAsync(PushPermissionStatus.NotDetermined);
        _permission.Setup(p => p.RequestAsync()).ReturnsAsync(PushPermissionStatus.Granted);

        var result = await _service.EnableAsync();

        Assert.That(result.Value, Is.EqualTo(PushPermissionStatus.Granted));
        _devices.Verify(d => d.RegisterAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task Enable_DeniedPermanentlyDetectedOnlyAfterRequest_OpensSettings() // zablokowane poza aplikacją
    {
        _permission.Setup(p => p.CheckAsync()).ReturnsAsync(PushPermissionStatus.NotDetermined);
        _permission.Setup(p => p.RequestAsync()).ReturnsAsync(PushPermissionStatus.DeniedPermanently);

        await _service.EnableAsync();

        _permissions.Verify(p => p.OpenAppSettings(), Times.Once);
    }

    [Test]
    public async Task GetStatus_ReadsPermission_WithoutRegistering() // rejestruje tylko PushStartup i „Włącz”
    {
        _permission.Setup(p => p.CheckAsync()).ReturnsAsync(PushPermissionStatus.Granted);

        var status = await _service.GetStatusAsync();

        Assert.That(status, Is.EqualTo(PushPermissionStatus.Granted));
        _devices.VerifyNoOtherCalls();
    }

    [Test]
    public async Task Enable_UserRefuses_DoesNotRegister()
    {
        _permission.Setup(p => p.CheckAsync()).ReturnsAsync(PushPermissionStatus.NotDetermined);
        _permission.Setup(p => p.RequestAsync()).ReturnsAsync(PushPermissionStatus.DeniedPermanently);

        await _service.EnableAsync();

        _devices.Verify(d => d.RegisterAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}

[TestFixture]
public class ForegroundPushHandlerTests
{
    private IMessenger _messenger = null!;
    private Mock<IInAppNotifier> _notifier = null!;
    private Mock<IDeepLinkRouter> _deepLinks = null!;
    private readonly Guid _reportId = Guid.NewGuid();

    [SetUp]
    public void SetUp()
    {
        _messenger = new WeakReferenceMessenger();
        _notifier = new Mock<IInAppNotifier>();
        _deepLinks = new Mock<IDeepLinkRouter>();
        var localizer = new Mock<ILocalizer>();
        localizer.SetupGet(l => l.CurrentCulture).Returns(System.Globalization.CultureInfo.GetCultureInfo("pl-PL"));
        localizer.Setup(l => l.GetString(It.IsAny<string>())).Returns((string key) => key switch
        {
            "Push_report_assigned" => "Nowe zgłoszenie: {0}",
            "Push_report_status_changed" => "Dyspozytor zmienił status: {0}",
            "Push_UnknownTitle" => "zgłoszenie",
            _ => key
        });
        _sessions = new Mock<ISessionStore>();
        _sessions.Setup(s => s.GetAsync()).ReturnsAsync(new Session("a@b.pl", "A", "t", "r"));
        _handler = new ForegroundPushHandler(new PushMessageRouter(), _deepLinks.Object, _notifier.Object, _messenger, localizer.Object,
            _sessions.Object);
        _handler.Start();
    }

    private ForegroundPushHandler _handler = null!;
    private Mock<ISessionStore> _sessions = null!;

    private PushReceivedMessage Message(string type, string? body) =>
        new(new Dictionary<string, string> { ["type"] = type, ["reportId"] = _reportId.ToString() }, body);

    [TestCase("report_assigned", "Awaria pompy", "Nowe zgłoszenie: Awaria pompy")]
    [TestCase("report_status_changed", "Awaria pompy", "Dyspozytor zmienił status: Awaria pompy")]
    [TestCase("report_assigned", null, "Nowe zgłoszenie: zgłoszenie")]
    public void ShowsBannerWithText(string type, string? body, string expected)
    {
        _messenger.Send(Message(type, body));
        _notifier.Verify(n => n.ShowAsync(expected, "Push_Show", It.IsAny<Func<Task>>()), Times.Once);
    }

    [Test]
    public void RequestsListRefresh()
    {
        var refreshed = 0;
        _messenger.Register<ReportsChangedMessage>(this, (_, _) => refreshed++);

        _messenger.Send(Message("report_assigned", "x"));

        Assert.That(refreshed, Is.EqualTo(1)); // lista pobierze nowe zgłoszenie z API
    }

    [Test]
    public async Task BannerAction_OpensReport()
    {
        Func<Task>? action = null;
        _notifier.Setup(n => n.ShowAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Func<Task>>()))
                 .Callback<string, string?, Func<Task>?>((_, _, a) => action = a).Returns(Task.CompletedTask);

        _messenger.Send(Message("report_assigned", "x"));
        await action!();

        _deepLinks.Verify(d => d.RouteAsync(new DeepLink(_reportId)), Times.Once);
    }

    [Test]
    public async Task SignedOut_ShowsNoBanner()
    {
        _sessions.Setup(s => s.GetAsync()).ReturnsAsync((Session?)null);

        _messenger.Send(Message("report_assigned", "Poufne"));
        await Task.Delay(20);

        _notifier.Verify(n => n.ShowAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Func<Task>>()), Times.Never);
    }

    [Test]
    public void UnknownPayload_IsIgnored()
    {
        _messenger.Send(new PushReceivedMessage(new Dictionary<string, string> { ["type"] = "promo" }));
        _notifier.Verify(n => n.ShowAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Func<Task>>()), Times.Never);
    }
}

[TestFixture]
public class AccountServiceTests
{
    [Test]
    public async Task SignOut_UnregistersDevice_ThenClearsSession()
    {
        var auth = new Mock<IAuthService>();
        var devices = new Mock<IDeviceRegistrationService>();
        var sequence = new MockSequence();
        devices.InSequence(sequence).Setup(d => d.UnregisterAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        auth.InSequence(sequence).Setup(a => a.SignOutAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var reports = new Mock<IReportRepository>();
        reports.InSequence(sequence).Setup(r => r.DeleteAllAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var photos = new Mock<IPhotoStorage>();
        await new AccountService(auth.Object, devices.Object, reports.Object, photos.Object).SignOutAsync();
        photos.Verify(p => p.DeleteAllAsync(It.IsAny<CancellationToken>()), Times.Once); // także zdjęcia

        auth.Verify(a => a.SignOutAsync(It.IsAny<CancellationToken>()), Times.Once);
        reports.Verify(r => r.DeleteAllAsync(It.IsAny<CancellationToken>()), Times.Once); // dane technika nie zostają
    }
}
