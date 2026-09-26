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

    public Task AlertAsync(string title, string message, string cancel) =>
        MainThread.InvokeOnMainThreadAsync(async () =>
        {
            if (_isOpen) return;
            _isOpen = true;
            try
            {
                var dialog = new DialogPage(title, message, cancel);
                await CurrentNavigation.PushModalAsync(dialog, false);
                await dialog.Result;
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
