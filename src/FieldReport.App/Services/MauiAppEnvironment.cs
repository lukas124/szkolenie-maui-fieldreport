using FieldReport.Core.Services;
using Microsoft.Extensions.Options;

namespace FieldReport.App.Services;

/// <summary>Sekcja „Environment” z appsettings.{Configuration}.json.</summary>
public sealed class AppEnvironmentSettings
{
    public string Name { get; set; } = "PROD";
}

/// <summary>
/// Środowisko z konfiguracji (DEV / TEST / PROD), wersja z buildu: AppInfo = ApplicationDisplayVersion (1.0.0)
/// + ApplicationVersion (42) z csproj.
/// </summary>
public sealed class MauiAppEnvironment(IOptions<AppEnvironmentSettings> settings) : IAppEnvironment
{
    public string EnvironmentName => settings.Value.Name;

    public string VersionText => $"{AppInfo.Current.VersionString} ({AppInfo.Current.BuildString})";

#if DEBUG
    public bool IsDebug => true;
#else
    public bool IsDebug => false;
#endif

    public string PlatformName => DeviceInfo.Current.Platform == DevicePlatform.iOS ? "ios" : "android";
}
