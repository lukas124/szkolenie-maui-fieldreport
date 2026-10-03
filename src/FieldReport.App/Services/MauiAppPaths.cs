using FieldReport.Core.Services;

namespace FieldReport.App.Services;

public sealed class MauiAppPaths : IAppPaths
{
    public string DataDirectory => FileSystem.AppDataDirectory;
}
