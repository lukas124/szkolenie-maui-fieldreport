using FieldReport.Core.Data.Api;
using FieldReport.Core.Models;

namespace FieldReport.Core.Services.Push;

/// <summary>
/// Przypisuje telefon do zalogowanego technika w API: wysyła token FCM (adres urządzenia w Firebase), żeby
/// dyspozytor mógł wysłać mu powiadomienie. Brak tokenu (iOS bez konfiguracji push, emulator bez Google Play)
/// nie jest błędem — rejestrację pomijamy, a aplikacja działa dalej.
/// </summary>
public sealed class DeviceRegistrationService(
    IPushTokenProvider tokens,
    IDevicesApiClient api,
    IPushPermissionService permission,
    ISessionStore sessions,
    IAppEnvironment environment) : IDeviceRegistrationService
{
    // POST api/devices z tokenem i platformą („android” / „ios”). API przypisuje token do technika z tokenu logowania.
    public async Task<Result> RegisterAsync(CancellationToken ct = default)
    {
        var token = await tokens.GetTokenAsync(ct);
        if (string.IsNullOrEmpty(token)) return Result.Success(); // brak konfiguracji push: pomijamy bez błędu

        return await api.RegisterAsync(token, environment.PlatformName, ct);
    }

    // Rejestracja „gdy można”: wołana przy starcie, po powrocie z tła i po zmianie tokenu (np. zgodę włączono
    // w ustawieniach systemu). Bez zalogowanego technika albo bez zgody nie ma czego rejestrować.
    public async Task<Result> RegisterIfPermittedAsync(CancellationToken ct = default)
    {
        if (await sessions.GetAsync() is null) return Result.Success();
        if (await permission.CheckAsync() != PushPermissionStatus.Granted) return Result.Success();

        return await RegisterAsync(ct);
    }

    // wołane z PushStartup.Start() i PushStartup.OnResumed()
    public async Task RefreshAsync(CancellationToken ct = default)
    {
        if (await sessions.GetAsync() is null)
        {
            // wylogowany telefon nie może dostawać powiadomień
            await tokens.DeleteTokenAsync(ct);
            return;
        }
        await RegisterIfPermittedAsync(ct);
    }

    // Wylogowanie: DELETE api/devices/{token} („best effort” — offline też można
    // się wylogować), a potem usunięcie tokenu w Firebase na telefonie: stary
    // token przestaje działać, więc powiadomienia już nie dotrą.
    public async Task UnregisterAsync(CancellationToken ct = default)
    {
        var token = await tokens.GetTokenAsync(ct);
        if (string.IsNullOrEmpty(token)) return;

        await api.UnregisterAsync(token, ct); // wynik ignorujemy
        await tokens.DeleteTokenAsync(ct);
    }
}
