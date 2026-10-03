namespace FieldReport.Core.Services;

/// <summary>Dialogi systemowe. Implementacja w App (MAUI), w testach mock.</summary>
public interface IDialogService
{
    Task AlertAsync(string title, string message, string cancel);
    Task<bool> ConfirmAsync(string title, string message, string accept, string cancel);
}
