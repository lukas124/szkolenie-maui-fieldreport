using FieldReport.Core.Services;

namespace FieldReport.App.Services;

/// <summary>Środowisko i wersja z buildu (AppInfo = ApplicationDisplayVersion + ApplicationVersion z csproj).</summary>
public sealed class MauiAppEnvironment : IAppEnvironment
{
#if DEBUG
    public string EnvironmentName => "DEV";
#else
    public string EnvironmentName => "PROD";
#endif

    public string VersionText => $"{AppInfo.Current.VersionString} ({AppInfo.Current.BuildString})";

#if DEBUG
    public bool IsDebug => true;
#else
    public bool IsDebug => false;
#endif

    public string PlatformName => DeviceInfo.Current.Platform == DevicePlatform.iOS ? "ios" : "android";
}
