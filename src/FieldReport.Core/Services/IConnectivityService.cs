namespace FieldReport.Core.Services;

/// <summary>Stan sieci za interfejsem (App: Connectivity.Current). W testach: offline jednym ustawieniem mocka.</summary>
public interface IConnectivityService
{
    bool IsOnline { get; }
}
