using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using FieldReport.App.Extensions;
using FieldReport.Core.Services;
using FieldReport.Core.Services.Push;

namespace FieldReport.App.Push;

/// <summary>Baner w aplikacji jako Snackbar (CommunityToolkit.Maui) na dole ekranu, z akcją „Pokaż”.</summary>
public sealed class SnackbarInAppNotifier(IErrorHandler errors) : IInAppNotifier
{
    public Task ShowAsync(string text, string? actionText = null, Func<Task>? action = null) =>
        MainThread.InvokeOnMainThreadAsync(() =>
        {
            Action? onTap = action is null ? null : () => action().FireAndForget(errors);
            var options = new SnackbarOptions
            {
                BackgroundColor = Color.FromArgb("#1B2126"),
                TextColor = Color.FromArgb("#E6EAED"),
                ActionButtonTextColor = Color.FromArgb("#E07A45"),
                CornerRadius = new CornerRadius(10),
                Font = Microsoft.Maui.Font.SystemFontOfSize(15),
                ActionButtonFont = Microsoft.Maui.Font.SystemFontOfSize(15, FontWeight.Bold)
            };
            return Snackbar.Make(text, onTap, actionText ?? string.Empty, TimeSpan.FromSeconds(6), options).Show();
        });
}
