using System.ComponentModel;
using System.Globalization;
using System.Resources;
using FieldReport.Core.Services;

namespace FieldReport.App.Services.Localization;

/// <summary>
/// Czyta teksty z plików Resources/Localizations/Strings*.resx przez ResourceManager.
/// Strings.resx to język domyślny (polski), Strings.en.resx — angielski. Nowy język = nowy plik Strings.{kod}.resx.
/// </summary>
public sealed class Localizer : ILocalizer, INotifyPropertyChanged
{
    // Nazwa zasobu = domyślna przestrzeń nazw projektu + ścieżka pliku bez rozszerzenia
    private readonly ResourceManager _resources =
        new("FieldReport.App.Resources.Localizations.Strings", typeof(Localizer).Assembly);

    public event PropertyChangedEventHandler? PropertyChanged;

    // Wybrany język zapamiętujemy w Preferences; przy pierwszym uruchomieniu — polski
    private const string LanguageKey = "language";

    public CultureInfo CurrentCulture { get; private set; } =
        CultureInfo.GetCultureInfo(Preferences.Default.Get(LanguageKey, "pl-PL"));

    // Indeksator dla bindingu z XAML: {loc:Translate Klucz} tworzy Binding do ścieżki „[Klucz]” tego obiektu
    public string this[string key] => GetString(key);

    public string GetString(string key) => _resources.GetString(key, CurrentCulture) ?? key;

    public void SetLocale(CultureInfo culture)
    {
        CurrentCulture = culture;
        Preferences.Default.Set(LanguageKey, culture.Name);
        // Pusta nazwa właściwości = „zmieniło się wszystko”: każdy binding do indeksatora pobierze tekst ponownie
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
    }
}
