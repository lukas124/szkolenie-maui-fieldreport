namespace FieldReport.Core.Services;

/// <summary>Bieżący czas za interfejsem: w testach podstawiamy stałą datę.</summary>
public interface IClock
{
    DateTimeOffset Now { get; }
}
