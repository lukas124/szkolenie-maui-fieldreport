using FieldReport.Core.Services;

namespace FieldReport.App.Services;

/// <summary>Czy telefon ma dostęp do internetu (Connectivity z MAUI).</summary>
public sealed class MauiConnectivityService : IConnectivityService
{
    public bool IsOnline => Connectivity.Current.NetworkAccess == NetworkAccess.Internet;
}
