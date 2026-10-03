using FieldReport.Core.Navigation;
using FieldReport.Core.Services;

namespace FieldReport.Core.ViewModels;

/// <summary>
/// Start aplikacji: splash widoczny od razu, routing w tle. Reset stosu = strona startowa nie zostaje na stosie
/// (wstecz z listy zamyka aplikację). Link z powiadomienia, które uruchomiło aplikację, dopiero po starcie.
/// </summary>
public sealed class StartupViewModel(
    INavigationService navigation, IAuthService auth, IDeepLinkRouter deepLinks) : BaseViewModel(navigation)
{
    public override async Task OnNavigatedToAsync(object? parameter)
    {
        var signedIn = await auth.HasValidSessionAsync();
        if (signedIn) await Navigation.ResetToAsync<ReportsViewModel>();
        else await Navigation.ResetToAsync<LoginViewModel>();
        await deepLinks.OnStartupCompletedAsync(signedIn); // powiadomienia push: link z zimnego startu
    }
}
