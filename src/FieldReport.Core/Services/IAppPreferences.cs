namespace FieldReport.Core.Services;

/// <summary>Proste ustawienia (Preferences): nie sekrety, nie dane. Implementacja w App.</summary>
public interface IAppPreferences
{
    /// <summary>Czy pokazano już ekran „Włącz powiadomienia” (pokazujemy go tylko po pierwszym logowaniu).</summary>
    bool NotificationPrimingShown { get; set; }
}
