using FieldReport.Core.Models;

namespace FieldReport.Core.Services.Push;

/// <summary>Powiadomienia w Ustawieniach: stan, „Włącz” (prośba albo ustawienia systemu przy trwałej odmowie), token (Debug).</summary>
public interface INotificationSettingsService
{
    /// <summary>Bieżący stan zgody na powiadomienia (bez rejestracji urządzenia).</summary>
    Task<PushPermissionStatus> GetStatusAsync();
    Task<Result<PushPermissionStatus>> EnableAsync(CancellationToken ct = default);

    /// <summary>(Debug) Kopiuje token FCM do schowka — do testu z konsoli Firebase. false = brak tokenu na tym urządzeniu.</summary>
    Task<bool> CopyTokenAsync(CancellationToken ct = default);
}
