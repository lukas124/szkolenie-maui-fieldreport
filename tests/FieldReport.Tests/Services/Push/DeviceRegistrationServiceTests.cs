using FieldReport.Core.Data.Api;
using FieldReport.Core.Models;
using FieldReport.Core.Services;
using FieldReport.Core.Services.Push;
using FieldReport.Tests.TestData;

namespace FieldReport.Tests.Services.Push;

[TestFixture]
public class DeviceRegistrationServiceTests
{
    private Mock<IPushTokenProvider> _tokens = null!;
    private Mock<IDevicesApiClient> _api = null!;
    private Mock<IPushPermissionService> _permission = null!;
    private Mock<ISessionStore> _sessions = null!;
    private DeviceRegistrationService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _tokens = new Mock<IPushTokenProvider>();
        _api = new Mock<IDevicesApiClient>();
        _permission = new Mock<IPushPermissionService>();
        _sessions = new Mock<ISessionStore>();
        _api.Setup(a => a.RegisterAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success());
        _api.Setup(a => a.UnregisterAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success());
        _service = new DeviceRegistrationService(_tokens.Object, _api.Object, _permission.Object, _sessions.Object,
            Mock.Of<IAppEnvironment>(e => e.PlatformName == "android"));
    }

    private void SignedInWithPermission()
    {
        _sessions.Setup(s => s.GetAsync()).ReturnsAsync(new Session("jan@firma.pl", "Jan", "a", "r"));
        _permission.Setup(p => p.CheckAsync()).ReturnsAsync(PushPermissionStatus.Granted);
        _tokens.Setup(t => t.GetTokenAsync(It.IsAny<CancellationToken>())).ReturnsAsync("token-1");
    }

    [Test]
    public async Task Register_SendsTokenWithPlatform()
    {
        _tokens.Setup(t => t.GetTokenAsync(It.IsAny<CancellationToken>())).ReturnsAsync("token-1");

        var result = await _service.RegisterAsync();

        Assert.That(result.IsSuccess, Is.True);
        _api.Verify(a => a.RegisterAsync("token-1", "android", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task Register_NoToken_IsSkippedWithoutError() // iOS bez konfiguracji push, emulator bez Google Play
    {
        _tokens.Setup(t => t.GetTokenAsync(It.IsAny<CancellationToken>())).ReturnsAsync((string?)null);

        var result = await _service.RegisterAsync();

        Assert.That(result.IsSuccess, Is.True);
        _api.Verify(a => a.RegisterAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task Register_Offline_ReturnsNetworkError()
    {
        _tokens.Setup(t => t.GetTokenAsync(It.IsAny<CancellationToken>())).ReturnsAsync("token-1");
        _api.Setup(a => a.RegisterAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure(Errors.Network));

        var result = await _service.RegisterAsync();

        Assert.That(result.Error, Is.EqualTo(Errors.Network));
    }

    [Test]
    public async Task RegisterIfPermitted_SignedOut_DoesNothing()
    {
        await _service.RegisterIfPermittedAsync();

        _api.Verify(a => a.RegisterAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task RegisterIfPermitted_NoPermission_DoesNothing()
    {
        SignedInWithPermission();
        _permission.Setup(p => p.CheckAsync()).ReturnsAsync(PushPermissionStatus.NotDetermined);

        await _service.RegisterIfPermittedAsync();

        _api.Verify(a => a.RegisterAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task RegisterIfPermitted_SignedInAndGranted_Registers()
    {
        SignedInWithPermission();

        await _service.RegisterIfPermittedAsync();

        _api.Verify(a => a.RegisterAsync("token-1", "android", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task Unregister_RemovesTokenInApi_AndDeletesItLocally()
    {
        SignedInWithPermission();

        await _service.UnregisterAsync();

        _api.Verify(a => a.UnregisterAsync("token-1", It.IsAny<CancellationToken>()), Times.Once);
        _tokens.Verify(t => t.DeleteTokenAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task Unregister_Offline_StillDeletesTokenLocally()
    {
        SignedInWithPermission();
        _api.Setup(a => a.UnregisterAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(Result.Failure(Errors.Network));

        await _service.UnregisterAsync();

        _tokens.Verify(t => t.DeleteTokenAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task Unregister_NoToken_IsNoOp()
    {
        await _service.UnregisterAsync();

        _api.Verify(a => a.UnregisterAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task Refresh_SignedOut_DeletesFcmToken() // np. wylogowanie offline: przy starcie / powrocie z tła
    {
        await _service.RefreshAsync();

        _tokens.Verify(t => t.DeleteTokenAsync(It.IsAny<CancellationToken>()), Times.Once);
        _api.Verify(a => a.RegisterAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task Refresh_SignedInAndGranted_Registers_WithoutDeletingToken()
    {
        SignedInWithPermission();

        await _service.RefreshAsync();

        _api.Verify(a => a.RegisterAsync("token-1", "android", It.IsAny<CancellationToken>()), Times.Once);
        _tokens.Verify(t => t.DeleteTokenAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
