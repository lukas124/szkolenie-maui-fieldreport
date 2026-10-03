using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FieldReport.Core.Navigation;
using FieldReport.Core.Presentation;
using FieldReport.Core.Services;

namespace FieldReport.Core.ViewModels;

/// <summary>
/// Logowanie technika. ViewModel orkiestruje: reguła formatu → ICredentialsValidator, logowanie → IAuthService,
/// tekst błędu → IUserFeedback. Po sukcesie reset stosu do listy (nie da się wrócić wstecz do logowania).
/// </summary>
public sealed partial class LoginViewModel(
    INavigationService navigation,
    IAuthService auth,
    ICredentialsValidator validator,
    IUserFeedback feedback) : BaseViewModel(navigation)
{
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SignInCommand))]
    private string _email = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SignInCommand))]
    private string _pin = string.Empty;

    private bool CanSignIn() => validator.IsValid(Email, Pin);

    [RelayCommand(CanExecute = nameof(CanSignIn))]
    private async Task SignInAsync(CancellationToken ct)
    {
        IsBusy = true;
        try
        {
            var result = await auth.SignInAsync(Email.Trim(), Pin, ct);
            if (!result.IsSuccess)
            {
                await feedback.ShowErrorAsync(result.Error!);
                return;
            }
        }
        finally
        {
            IsBusy = false;
        }

        await Navigation.ResetToAsync<ReportsViewModel>();
    }
}
