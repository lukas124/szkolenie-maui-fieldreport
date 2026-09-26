using System.Net.Http.Json;
using FieldReport.Core.Models;

namespace FieldReport.Core.Data.Api;

/// <summary>Typed client logowania (bez nagłówka Bearer). Ścieżki względne — BaseAddress kończy się „/”.</summary>
public sealed class AuthApiClient(HttpClient http) : IAuthApiClient
{
    public Task<Result<TokenResponseDto>> LoginAsync(string email, string pin, CancellationToken ct = default) =>
        http.SendForResultAsync<TokenResponseDto>(new HttpRequestMessage(HttpMethod.Post, "api/auth/login")
        {
            Content = JsonContent.Create(new LoginRequestDto(email, pin), options: ApiJson.Options)
        }, ct);

    public Task<Result<TokenResponseDto>> RefreshAsync(string refreshToken, CancellationToken ct = default) =>
        http.SendForResultAsync<TokenResponseDto>(new HttpRequestMessage(HttpMethod.Post, "api/auth/refresh")
        {
            Content = JsonContent.Create(new RefreshRequestDto(refreshToken), options: ApiJson.Options)
        }, ct);
}
