namespace FieldReport.Core.Data.Api;

/// <summary>Konfiguracja API szkoleniowego. <c>BaseUrl</c> MUSI kończyć się „/”, a ścieżki w klientach są względne (bez „/” na początku).</summary>
public sealed class ApiSettings
{
    /// <summary>Publiczne API szkoleniowe (repo lukas124/fieldreport-training-api). Lokalnie z emulatora: http://10.0.2.2:5080/</summary>
    public const string TrainingBaseUrl = "https://mobilny.dev/maui/podstawowe/";

    public string BaseUrl { get; set; } = TrainingBaseUrl;
}
