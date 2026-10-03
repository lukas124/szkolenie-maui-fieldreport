using FieldReport.Core.Services;

namespace FieldReport.App.Services;

public sealed class MauiClipboardService : IClipboardService
{
    public Task SetTextAsync(string text) => MainThread.InvokeOnMainThreadAsync(() => Clipboard.Default.SetTextAsync(text));
}
