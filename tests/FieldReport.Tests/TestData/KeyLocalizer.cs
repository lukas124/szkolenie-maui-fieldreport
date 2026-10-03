using System.Globalization;
using FieldReport.Core.Services;

namespace FieldReport.Tests.TestData;

/// <summary>Lokalizator testowy: zwraca klucz zamiast tekstu, więc testy nie zależą od treści plików zasobów.</summary>
internal sealed class KeyLocalizer : ILocalizer
{
    public CultureInfo CurrentCulture => CultureInfo.GetCultureInfo("pl-PL");
    public string GetString(string key) => key;
    public void SetLocale(CultureInfo culture) { }
}
