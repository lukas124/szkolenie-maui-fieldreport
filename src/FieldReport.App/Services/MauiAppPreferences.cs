using FieldReport.Core.Services;

namespace FieldReport.App.Services;

/// <summary>Ustawienia aplikacji w Preferences (klucz → wartość, zostają po restarcie aplikacji).</summary>
public sealed class MauiAppPreferences : IAppPreferences
{
    public bool NotificationPrimingShown
    {
        get => Preferences.Default.Get("notifications.priming_shown", false);
        set => Preferences.Default.Set("notifications.priming_shown", value);
    }
}
