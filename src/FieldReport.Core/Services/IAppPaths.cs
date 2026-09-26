namespace FieldReport.Core.Services;

/// <summary>Katalogi aplikacji. Implementacja w App: FileSystem.AppDataDirectory; w testach katalog tymczasowy.</summary>
public interface IAppPaths
{
    /// <summary>Dane trwałe (baza, zdjęcia) — usuwane dopiero z aplikacją.</summary>
    string DataDirectory { get; }
}
