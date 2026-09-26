using FieldReport.Core.Models;

namespace FieldReport.Core.Presentation;

/// <summary>Komunikacja wyniku z użytkownikiem (dialogi z tekstami z zasobów).</summary>
public interface IUserFeedback
{
    Task ShowErrorAsync(Error error);

    /// <summary>Pytanie tak/nie; argumenty to klucze zasobów (teksty z ILocalizer).</summary>
    Task<bool> ConfirmAsync(string titleKey, string messageKey);
}
