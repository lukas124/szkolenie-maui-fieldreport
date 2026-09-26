namespace FieldReport.Core.Services.Push;

/// <summary>Baner w aplikacji (App: Snackbar z CommunityToolkit.Maui). Tekst gotowy, akcja opcjonalna.</summary>
public interface IInAppNotifier
{
    Task ShowAsync(string text, string? actionText = null, Func<Task>? action = null);
}
