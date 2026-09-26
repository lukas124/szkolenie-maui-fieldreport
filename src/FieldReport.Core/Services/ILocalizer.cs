using System.Globalization;

namespace FieldReport.Core.Services;

/// <summary>
/// Teksty dla użytkownika z plików zasobów (Strings.resx). Interfejs jest w Core, implementacja (Localizer) w App,
/// dzięki czemu ViewModele i ich testy nie zależą od plików zasobów ani od MAUI.
/// </summary>
public interface ILocalizer
{
    /// <summary>Bieżący język tekstów i format dat.</summary>
    CultureInfo CurrentCulture { get; }

    /// <summary>Tekst dla klucza w bieżącym języku. Gdy klucza nie ma w zasobach, zwraca sam klucz (widać go od razu na ekranie).</summary>
    string GetString(string key);

    /// <summary>Zmienia język aplikacji w locie (np. na „en”): teksty z <c>{loc:Translate}</c> odświeżają się same.</summary>
    void SetLocale(CultureInfo culture);
}
