using FieldReport.Core.Models;

namespace FieldReport.Core.Services.Push;

/// <summary>Powiadomienia w Ustawieniach: stan zgody, „Włącz” (prośba o zgodę albo ustawienia systemu) i token do testów.</summary>
public sealed class NotificationSettingsService(
    IPushPermissionService permission, IPermissionService permissions, IDeviceRegistrationService devices,
    IPushTokenProvider tokens, IClipboardService clipboard) : INotificationSettingsService
{
    // Tylko odczyt: urządzenie rejestruje PushStartup (start, powrót z tła) i EnableAsync
    public Task<PushPermissionStatus> GetStatusAsync() => permission.CheckAsync();

    public async Task<Result<PushPermissionStatus>> EnableAsync(CancellationToken ct = default)
    {
        var status = await permission.CheckAsync();
        if (status == PushPermissionStatus.NotDetermined) status = await permission.RequestAsync();
        // Odmowa na stałe (także wykryta dopiero po prośbie): system nie pokaże już okna zgody → ustawienia systemu
        if (status == PushPermissionStatus.DeniedPermanently) permissions.OpenAppSettings();
        if (status != PushPermissionStatus.Granted) return Result<PushPermissionStatus>.Success(status);

        var registered = await devices.RegisterAsync(ct);
        return registered.IsSuccess
            ? Result<PushPermissionStatus>.Success(status)
            : Result<PushPermissionStatus>.Failure(registered.Error!);
    }

    public async Task<bool> CopyTokenAsync(CancellationToken ct = default)
    {
        var token = await tokens.GetTokenAsync(ct);
        if (token is null) return false;

        await clipboard.SetTextAsync(token);
        return true;
    }
}
