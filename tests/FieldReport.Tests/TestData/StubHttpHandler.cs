using System.Net;
using System.Text;

namespace FieldReport.Tests.TestData;

/// <summary>Atrapa sieci dla typed clientów: zapisuje żądania i zwraca przygotowane odpowiedzi (albo rzuca wyjątek).</summary>
internal sealed class StubHttpHandler : HttpMessageHandler
{
    private readonly Queue<Func<HttpRequestMessage, HttpResponseMessage>> _responses = new();

    public List<(HttpRequestMessage Request, string? Body)> Requests { get; } = new();

    public StubHttpHandler Respond(HttpStatusCode status, string? json = null)
    {
        _responses.Enqueue(_ => new HttpResponseMessage(status)
        {
            Content = json is null ? null : new StringContent(json, Encoding.UTF8, "application/json")
        });
        return this;
    }

    public StubHttpHandler Throw(Exception exception)
    {
        _responses.Enqueue(_ => throw exception);
        return this;
    }

    public HttpClient CreateClient() => new(this) { BaseAddress = new Uri("https://api.test/maui/podstawowe/") };

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        var body = request.Content is null ? null : await request.Content.ReadAsStringAsync(ct);
        Requests.Add((request, body));
        return _responses.Count > 0 ? _responses.Dequeue()(request) : new HttpResponseMessage(HttpStatusCode.OK);
    }
}
