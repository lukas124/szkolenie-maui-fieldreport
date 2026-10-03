using FieldReport.Core.Models;
using FieldReport.Core.Services;

namespace FieldReport.App.Services;

/// <summary>
/// Uprawnienia MAUI (Permissions). Odmowa na stałe: Android — po drugiej odmowie system nie pokazuje już okna
/// (ShouldShowRationale = false); iOS — pyta tylko raz, potem wyłącznie Ustawienia.
/// </summary>
public sealed class MauiPermissionService : IPermissionService
{
    public async Task<Result> EnsureCameraAsync()
    {
        return await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            var status = await Permissions.CheckStatusAsync<Permissions.Camera>();
            if (status == PermissionStatus.Granted) return Result.Success();

            if (DeviceInfo.Platform == DevicePlatform.iOS && status == PermissionStatus.Denied)
                return Result.Failure(Errors.PermissionDeniedPermanently); // iOS nie zapyta drugi raz

            status = await Permissions.RequestAsync<Permissions.Camera>();
            if (status == PermissionStatus.Granted) return Result.Success();

            // Android: brak uzasadnienia po odmowie = „nie pytaj ponownie” (system już nie pokaże okna)
            if (DeviceInfo.Platform == DevicePlatform.Android && !Permissions.ShouldShowRationale<Permissions.Camera>())
                return Result.Failure(Errors.PermissionDeniedPermanently);

            return Result.Failure(Errors.PermissionDenied);
        });
    }

    public void OpenAppSettings() => AppInfo.Current.ShowSettingsUI();
}
