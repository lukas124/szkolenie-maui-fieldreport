namespace FieldReport.Core.Services;

/// <summary>Informacje o buildzie dla ekranu Ustawień: środowisko (DEV/TEST/PROD) i wersja „1.0.0 (42)”.</summary>
public interface IAppEnvironment
{
    string EnvironmentName { get; }
    string VersionText { get; }
}
