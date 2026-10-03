using System.Net.Http.Json;
using FieldReport.Core.Models;

namespace FieldReport.Core.Data.Api;

/// <summary>Rejestracja urządzenia do powiadomień push: POST api/devices, DELETE api/devices/{token} (token zakodowany w adresie).</summary>
public sealed class DevicesApiClient(HttpClient http) : IDevicesApiClient
{
    public Task<Result> RegisterAsync(string token, string platform, CancellationToken ct = default) =>
        http.SendForResultAsync(new HttpRequestMessage(HttpMethod.Post, "api/devices")
        {
            Content = JsonContent.Create(new RegisterDeviceRequestDto(token, platform), options: ApiJson.Options)
        }, ct);

    // token FCM zawiera „:” — zawsze kodujemy fragment adresu
    public Task<Result> UnregisterAsync(string token, CancellationToken ct = default) =>
        http.SendForResultAsync(new HttpRequestMessage(HttpMethod.Delete, $"api/devices/{Uri.EscapeDataString(token)}"), ct);
}
