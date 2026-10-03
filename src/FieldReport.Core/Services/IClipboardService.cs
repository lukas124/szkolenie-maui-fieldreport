namespace FieldReport.Core.Services;

/// <summary>Schowek systemowy (App: Clipboard.Default).</summary>
public interface IClipboardService
{
    Task SetTextAsync(string text);
}
