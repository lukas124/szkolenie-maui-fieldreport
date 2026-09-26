using FieldReport.Core.Models;

namespace FieldReport.Core.Services;

/// <summary>Uprawnienia w runtime (deklaracje: AndroidManifest.xml, Info.plist). Implementacja w App.</summary>
public interface IPermissionService
{
    /// <summary>Sprawdza i w razie potrzeby prosi o dostęp do aparatu.</summary>
    Task<Result> EnsureCameraAsync();

    /// <summary>Otwiera ustawienia aplikacji w systemie (po odmowie na stałe tylko tam można nadać uprawnienie).</summary>
    void OpenAppSettings();
}
