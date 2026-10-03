using FieldReport.App.Pages;
using FieldReport.Core.Services;

namespace FieldReport.App.Services;

/// <summary>
/// Dialogi jako własna strona modalna (DialogPage) — ten sam wygląd na Androidzie i iOS.
/// Zawsze na wątku UI; stos modalny należy do okna, więc dialog przykrywa także inną stronę modalną.
/// </summary>
public sealed class MauiDialogService : IDialogService
{
    // true = dialog jest na ekranie; kolejny (np. drugi błąd w tej samej chwili) nie jest pokazywany
    private bool _isOpen;

    public Task AlertAsync(string title, string message, string cancel) => ShowAsync(title, message, cancel, null);

    public Task<bool> ConfirmAsync(string title, string message, string accept, string cancel) =>
        ShowAsync(title, message, accept, cancel);

    private Task<bool> ShowAsync(string title, string message, string accept, string? cancel) =>
        MainThread.InvokeOnMainThreadAsync(async () =>
        {
            if (_isOpen) return false;
            _isOpen = true;
            try
            {
                var dialog = new DialogPage(title, message, accept, cancel);
                await CurrentNavigation.PushModalAsync(dialog, false);
                return await dialog.Result;
            }
            finally
            {
                _isOpen = false;
            }
        });

    private static INavigation CurrentNavigation =>
        Application.Current?.Windows.FirstOrDefault()?.Page?.Navigation
        ?? throw new InvalidOperationException("No window page to show a dialog on.");
}
