using System.Net;
using CommunityToolkit.Mvvm.Messaging;
using FieldReport.Core.Data.Api;
using FieldReport.Core.Models;
using FieldReport.Core.Services;
using FieldReport.Tests.TestData;

namespace FieldReport.Tests.Data.Api;

[TestFixture]
public class ApiClientsTests
{
    [Test]
    public async Task Login_PostsCamelCaseJson_ToRelativePathUnderBase()
    {
        var handler = new StubHttpHandler().Respond(HttpStatusCode.OK,
            """{"accessToken":"a","refreshToken":"r","expiresIn":600,"technicianName":"Trener Dev"}""");

        var result = await new AuthApiClient(handler.CreateClient()).LoginAsync("trener.dev@example.com", "1234");

        Assert.That(result.Value, Is.EqualTo(new TokenResponseDto("a", "r", 600, "Trener Dev")));
        var (request, body) = handler.Requests.Single();
        Assert.That(request.RequestUri!.ToString(), Is.EqualTo("https://api.test/maui/podstawowe/api/auth/login")); // PathBase zachowany
        Assert.That(body, Is.EqualTo("""{"email":"trener.dev@example.com","pin":"1234"}"""));
    }

    [Test]
    public async Task Login_401_IsUnauthorized()
    {
        var handler = new StubHttpHandler().Respond(HttpStatusCode.Unauthorized);
        var result = await new AuthApiClient(handler.CreateClient()).LoginAsync("a@b.pl", "0000");
        Assert.That(result.Error, Is.EqualTo(Errors.Unauthorized));
    }

    [Test]
    public async Task NetworkFailure_IsNetworkError()
    {
        var handler = new StubHttpHandler().Throw(new HttpRequestException("no route"));
        var result = await new AuthApiClient(handler.CreateClient()).LoginAsync("a@b.pl", "1234");
        Assert.That(result.Error, Is.EqualTo(Errors.Network));
    }

    [Test]
    public async Task Timeout_IsNetworkError()
    {
        var handler = new StubHttpHandler().Throw(new TaskCanceledException("timeout"));
        var result = await new AuthApiClient(handler.CreateClient()).LoginAsync("a@b.pl", "1234");
        Assert.That(result.Error, Is.EqualTo(Errors.Network));
    }

    [Test]
    public void CallerCancellation_IsNotSwallowed()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var handler = new StubHttpHandler().Throw(new TaskCanceledException());
        Assert.That(() => new AuthApiClient(handler.CreateClient()).LoginAsync("a@b.pl", "1234", cts.Token),
            Throws.InstanceOf<OperationCanceledException>());
    }

    [Test]
    public async Task InvalidJson_IsInvalidResponse()
    {
        var handler = new StubHttpHandler().Respond(HttpStatusCode.OK, "<html>proxy error</html>");
        var result = await new AuthApiClient(handler.CreateClient()).LoginAsync("a@b.pl", "1234");
        Assert.That(result.Error, Is.EqualTo(Errors.InvalidResponse));
    }

    [TestCase(HttpStatusCode.BadRequest, "invalid_request")]
    [TestCase(HttpStatusCode.NotFound, "not_found")]
    [TestCase(HttpStatusCode.Conflict, "conflict")]
    [TestCase(HttpStatusCode.InternalServerError, "server")]
    [TestCase(HttpStatusCode.BadGateway, "server")]
    [TestCase(HttpStatusCode.TooManyRequests, "network")]
    [TestCase(HttpStatusCode.Forbidden, "unknown")]
    public void StatusCodes_AreMapped(HttpStatusCode status, string code) =>
        Assert.That(HttpErrorMapper.Map(status).Code, Is.EqualTo(code));

    [Test]
    public async Task RegisterDevice_PostsTokenAndPlatform()
    {
        var handler = new StubHttpHandler().Respond(HttpStatusCode.OK, "{}");
        var result = await new DevicesApiClient(handler.CreateClient()).RegisterAsync("fX:APA91", "android");

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(handler.Requests.Single().Body, Is.EqualTo("""{"token":"fX:APA91","platform":"android"}"""));
    }

    [Test]
    public async Task UnregisterDevice_EncodesTokenInPath()
    {
        var handler = new StubHttpHandler().Respond(HttpStatusCode.NoContent);
        await new DevicesApiClient(handler.CreateClient()).UnregisterAsync("fX:APA91/b+c");

        var request = handler.Requests.Single().Request;
        Assert.That(request.Method, Is.EqualTo(HttpMethod.Delete));
        Assert.That(request.RequestUri!.AbsolutePath, Is.EqualTo("/maui/podstawowe/api/devices/fX%3AAPA91%2Fb%2Bc"));
    }

    [Test]
    public void BaseUrl_EndsWithSlash() => Assert.That(ApiSettings.TrainingBaseUrl, Does.EndWith("/"));
}

[TestFixture]
public class ApiAuthServiceTests
{
    private Mock<IAuthApiClient> _api = null!;
    private Mock<ISessionStore> _sessions = null!;
    private IMessenger _messenger = null!;
    private ApiAuthService _auth = null!;

    [SetUp]
    public void SetUp()
    {
        _api = new Mock<IAuthApiClient>();
        _sessions = new Mock<ISessionStore>();
        _messenger = new WeakReferenceMessenger();
        _auth = new ApiAuthService(_api.Object, _sessions.Object, _messenger);
    }

    [Test]
    public async Task SignIn_Success_SavesTokens_WithExpiry()
    {
        _api.Setup(a => a.LoginAsync("jan@firma.pl", "1234", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<TokenResponseDto>.Success(new TokenResponseDto("acc", "ref", 600, "Jan Kowalski")));

        var result = await _auth.SignInAsync(" jan@firma.pl ", "1234");

        Assert.That(result.IsSuccess, Is.True);
        _sessions.Verify(s => s.SaveAsync(new Session("jan@firma.pl", "Jan Kowalski", "acc", "ref")), Times.Once);
    }

    [Test]
    public async Task SignIn_401_IsInvalidCredentials()
    {
        _api.Setup(a => a.LoginAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<TokenResponseDto>.Failure(Errors.Unauthorized));

        Assert.That((await _auth.SignInAsync("a@b.pl", "9999")).Error, Is.EqualTo(Errors.InvalidCredentials));
        _sessions.Verify(s => s.SaveAsync(It.IsAny<Session>()), Times.Never);
    }

    [Test]
    public async Task SignIn_Offline_IsNetwork()
    {
        _api.Setup(a => a.LoginAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<TokenResponseDto>.Failure(Errors.Network));

        Assert.That((await _auth.SignInAsync("a@b.pl", "1234")).Error, Is.EqualTo(Errors.Network));
    }

    // ----- odświeżanie tokenu (401 → refresh → ponowienie) -----

    private static readonly Session Current = new("jan@firma.pl", "Jan Kowalski", "old-acc", "old-ref");

    [Test]
    public async Task Refresh_Success_StoresRotatedTokens()
    {
        _sessions.Setup(s => s.GetAsync()).ReturnsAsync(Current);
        _api.Setup(a => a.RefreshAsync("old-ref", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<TokenResponseDto>.Success(new TokenResponseDto("new-acc", "new-ref", 600, "Jan Kowalski")));

        var result = await _auth.RefreshSessionAsync("old-acc");

        Assert.That(result.IsSuccess, Is.True);
        _sessions.Verify(s => s.SaveAsync(It.Is<Session>(x => x.AccessToken == "new-acc" && x.RefreshToken == "new-ref" && x.Email == "jan@firma.pl")));
    }

    [Test]
    public async Task Refresh_Rejected_ClearsSession_AndAnnouncesExpiry()
    {
        var expired = 0;
        _messenger.Register<SessionExpiredMessage>(this, (_, _) => expired++);
        _sessions.Setup(s => s.GetAsync()).ReturnsAsync(Current);
        _api.Setup(a => a.RefreshAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<TokenResponseDto>.Failure(Errors.Unauthorized));

        var result = await _auth.RefreshSessionAsync("old-acc");

        Assert.That(result.Error, Is.EqualTo(Errors.SessionExpired));
        _sessions.Verify(s => s.ClearAsync(), Times.Once);
        Assert.That(expired, Is.EqualTo(1));
    }

    [Test]
    public async Task Refresh_Offline_KeepsSession()
    {
        _sessions.Setup(s => s.GetAsync()).ReturnsAsync(Current);
        _api.Setup(a => a.RefreshAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<TokenResponseDto>.Failure(Errors.Network));

        var result = await _auth.RefreshSessionAsync("old-acc");

        Assert.That(result.Error, Is.EqualTo(Errors.Network));
        _sessions.Verify(s => s.ClearAsync(), Times.Never);
    }

    [Test]
    public async Task Refresh_AlreadyRefreshedByAnotherRequest_DoesNotCallApi()
    {
        _sessions.Setup(s => s.GetAsync()).ReturnsAsync(Current with { AccessToken = "newer-acc" });

        var result = await _auth.RefreshSessionAsync("old-acc");

        Assert.That(result.IsSuccess, Is.True);
        _api.Verify(a => a.RefreshAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task Refresh_NoSession_IsExpired_WithoutCallingApi()
    {
        Assert.That((await _auth.RefreshSessionAsync("old-acc")).Error, Is.EqualTo(Errors.SessionExpired));
        _api.Verify(a => a.RefreshAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}

[TestFixture]
public class SessionExpiredHandlerTests
{
    [Test]
    public void SessionExpired_ResetsToLogin_AndExplains()
    {
        var messenger = new WeakReferenceMessenger();
        var navigation = new Mock<FieldReport.Core.Navigation.INavigationService>();
        var feedback = new Mock<FieldReport.Core.Presentation.IUserFeedback>();
        var handler = new SessionExpiredHandler(navigation.Object, feedback.Object, Mock.Of<IErrorHandler>(), messenger);
        handler.Start();

        messenger.Send(new SessionExpiredMessage());

        navigation.Verify(n => n.ResetToAsync<FieldReport.Core.ViewModels.LoginViewModel>(), Times.Once);
        feedback.Verify(f => f.ShowErrorAsync(Errors.SessionExpired), Times.Once);
        GC.KeepAlive(handler);
    }
}
