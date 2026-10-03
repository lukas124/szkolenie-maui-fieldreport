using CommunityToolkit.Mvvm.Messaging;
using FieldReport.Core.Models;
using FieldReport.Core.Navigation;
using FieldReport.Core.Presentation;
using FieldReport.Core.ViewModels;

namespace FieldReport.Core.Services;

/// <summary>Sesja wygasła (nieudane odświeżenie tokenu logowania) → reset stosu do logowania + komunikat. Singleton.</summary>
public sealed record SessionExpiredMessage;

public sealed class SessionExpiredHandler(
    INavigationService navigation, IUserFeedback feedback, IErrorHandler errors, IMessenger messenger) : IRecipient<SessionExpiredMessage>
{
    // Zaczyna nasłuch SessionExpiredMessage (wołane raz, przy starcie aplikacji)
    public void Start() => messenger.Register(this);

    public void Receive(SessionExpiredMessage message) => _ = GoToLoginAsync();

    private async Task GoToLoginAsync()
    {
        try
        {
            await navigation.ResetToAsync<LoginViewModel>();
            await feedback.ShowErrorAsync(Errors.SessionExpired);
        }
        catch (Exception ex)
        {
            errors.Handle(ex);
        }
    }
}
