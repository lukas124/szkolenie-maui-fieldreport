using System.Collections.Concurrent;
using System.Net;
using System.Text;
using CommunityToolkit.Mvvm.Messaging;
using FieldReport.Core.Data.Api;
using FieldReport.Core.Models;
using FieldReport.Core.Services;
using FieldReport.Tests.TestData;

namespace FieldReport.Tests.Data.Api;

/// <summary>AuthHeaderHandler + prawdziwy ApiAuthService na atrapie serwera: 401 → odświeżenie → jedno ponowienie.</summary>
[TestFixture]
public class AuthHeaderHandlerTests
{
    /// <summary>Atrapa API: ważny jest tylko bieżący access token; refresh token jednorazowy (jak w API szkoleniowym).</summary>
    private sealed class FakeServer : HttpMessageHandler
    {
        private readonly object _gate = new();
        public string ValidAccess = "acc-1";
        public string ValidRefresh = "ref-1";
        public int Refreshes;
        public ConcurrentBag<string?> SeenTokens { get; } = new();

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            await Task.Delay(10, ct); // równoległe żądania naprawdę się przeplatają
            if (request.RequestUri!.AbsolutePath.EndsWith("api/auth/refresh"))
            {
                var body = await request.Content!.ReadAsStringAsync(ct);
                lock (_gate)
                {
                    if (!body.Contains(ValidRefresh)) return new HttpResponseMessage(HttpStatusCode.Unauthorized);
                    Refreshes++;
                    ValidAccess = $"acc-{Refreshes + 1}";
                    ValidRefresh = $"ref-{Refreshes + 1}";
                    return Json($$"""{"accessToken":"{{ValidAccess}}","refreshToken":"{{ValidRefresh}}","expiresIn":600,"technicianName":"A"}""");
                }
            }

            var token = request.Headers.Authorization?.Parameter;
            SeenTokens.Add(token);
            lock (_gate) return token == ValidAccess ? Json("{}") : new HttpResponseMessage(HttpStatusCode.Unauthorized);
        }

        private static HttpResponseMessage Json(string json) =>
            new(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
    }

    private sealed class MemorySessionStore : ISessionStore
    {
        public Session? Current;
        public Task<Session?> GetAsync() => Task.FromResult(Current);
        public Task SaveAsync(Session session) { Current = session; return Task.CompletedTask; }
        public Task ClearAsync() { Current = null; return Task.CompletedTask; }
    }

    private FakeServer _server = null!;
    private MemorySessionStore _sessions = null!;
    private int _expired;
    private HttpClient _client = null!;

    [SetUp]
    public void SetUp()
    {
        _server = new FakeServer();
        _sessions = new MemorySessionStore { Current = new Session("a@b.pl", "A", "acc-1", "ref-1") };
        var messenger = new WeakReferenceMessenger();
        _expired = 0;
        messenger.Register<SessionExpiredMessage>(this, (_, _) => _expired++);
        var authApi = new AuthApiClient(new HttpClient(_server) { BaseAddress = new Uri("https://api.test/maui/podstawowe/") });
        var auth = new ApiAuthService(authApi, _sessions, messenger);
        _client = new HttpClient(new AuthHeaderHandler(_sessions, auth) { InnerHandler = _server })
            { BaseAddress = new Uri("https://api.test/maui/podstawowe/") };
    }

    [TearDown]
    public void TearDown()
    {
        _client.Dispose();
        _server.Dispose();
    }

    private Task<Result> CallAsync() => new DevicesApiClient(_client).RegisterAsync("tok:1", "android");

    [Test]
    public async Task ValidToken_IsSentAsBearer_WithoutRefresh()
    {
        Assert.That((await CallAsync()).IsSuccess, Is.True);
        Assert.That(_server.SeenTokens, Is.EqualTo(new[] { "acc-1" }));
        Assert.That(_server.Refreshes, Is.Zero);
    }

    [Test]
    public async Task ExpiredToken_401_RefreshesOnce_AndRetriesWithNewToken()
    {
        _server.ValidAccess = "acc-server-rotated"; // access token wygasł

        var result = await CallAsync();

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(_server.Refreshes, Is.EqualTo(1));
        Assert.That(_sessions.Current!.AccessToken, Is.EqualTo("acc-2"));
        Assert.That(_sessions.Current.RefreshToken, Is.EqualTo("ref-2"));
    }

    [Test]
    [Repeat(10)] // wyścig: kilka rund z przeplotem
    public async Task ParallelRequests_With401_RefreshOnlyOnce_AndAllSucceed() // refresh token jest jednorazowy
    {
        _server.ValidAccess = "acc-expired";

        var results = await Task.WhenAll(Enumerable.Range(0, 5).Select(_ => CallAsync()));

        Assert.That(results.All(r => r.IsSuccess), Is.True, string.Join(", ", results.Select(r => r.Error?.Code)));
        Assert.That(_server.Refreshes, Is.EqualTo(1));
        Assert.That(_expired, Is.Zero); // nikt nie został wylogowany
    }

    [Test]
    public async Task RefreshRejected_Returns401_ClearsSession_AndAnnouncesExpiry()
    {
        _server.ValidAccess = "acc-expired";
        _server.ValidRefresh = "ref-revoked";

        var result = await CallAsync();

        Assert.That(result.Error, Is.EqualTo(Errors.Unauthorized));
        Assert.That(_sessions.Current, Is.Null);
        Assert.That(_expired, Is.EqualTo(1));
    }

    [Test]
    public async Task NoSession_SendsWithoutHeader()
    {
        _sessions.Current = null;
        await CallAsync();
        Assert.That(_server.SeenTokens, Is.EqualTo(new string?[] { null }));
    }
}
