using System.Net;
using System.Net.Http.Headers;
using FieldReport.Core.Services;

namespace FieldReport.Core.Data.Api;

/// <summary>
/// „Pośrednik” w potoku HttpClient (DelegatingHandler): każde żądanie do API przechodzi przez tę klasę.
/// 1. Dokleja nagłówek „Authorization: Bearer …” z tokenem z sesji (SecureStorage).
/// 2. Gdy serwer odpowie 401 (token wygasł), odświeża token (IAuthService) i wysyła żądanie JESZCZE RAZ, jeden raz.
///    Nieudane odświeżenie → ApiAuthService usuwa sesję i aplikacja wraca do logowania.
/// </summary>
public sealed class AuthHeaderHandler(ISessionStore sessions, IAuthService auth) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        var used = await AttachTokenAsync(request);         // Authorization: Bearer …
        var response = await base.SendAsync(request, ct); // właściwe wysłanie żądania
        if (response.StatusCode != HttpStatusCode.Unauthorized || used is null) return response; // OK albo brak sesji

        // 401: nowy token (ochrona przed równoległymi odświeżeniami jest w ApiAuthService)
        var refreshed = await auth.RefreshSessionAsync(used, ct);
        if (!refreshed.IsSuccess) return response;

        response.Dispose();
        using var retry = await CloneAsync(request); // HttpRequestMessage nie da się wysłać drugi raz
        await AttachTokenAsync(retry);
        return await base.SendAsync(retry, ct);             // ponów z nowym tokenem
    }

    // Token z sesji do nagłówka; zwraca użyty token (null = nikt nie jest zalogowany)
    private async Task<string?> AttachTokenAsync(HttpRequestMessage request)
    {
        var session = await sessions.GetAsync();
        if (session is null) return null;
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", session.AccessToken);
        return session.AccessToken;
    }

    // Kopia żądania do ponowienia: adres, nagłówki (bez starego tokenu) i treść
    private static async Task<HttpRequestMessage> CloneAsync(HttpRequestMessage request)
    {
        var clone = new HttpRequestMessage(request.Method, request.RequestUri) { Version = request.Version };
        foreach (var header in request.Headers.Where(h => h.Key != "Authorization"))
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);

        if (request.Content is not null)
        {
            var bytes = await request.Content.ReadAsByteArrayAsync(); // treść zbuforowana
            clone.Content = new ByteArrayContent(bytes);
            foreach (var header in request.Content.Headers)
                clone.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }
        return clone;
    }
}
