using CommunityToolkit.Mvvm.Input;
using FieldReport.Core.Navigation;
using FieldReport.Core.Services;

namespace FieldReport.Core.ViewModels;

/// <summary>Start aplikacji: błąd podczas startu → ekran błędu z „Spróbuj ponownie”, nigdy crash.</summary>
public sealed partial class StartupErrorViewModel(INavigationService navigation, IErrorHandler errors) : BaseViewModel(navigation)
{
    [RelayCommand]
    private async Task RetryAsync()
    {
        try
        {
            await Navigation.ResetToAsync<StartupViewModel>();
        }
        catch (Exception ex)
        {
            errors.Handle(ex); // start znów się nie udał: wracamy na ekran błędu zamiast crasha
            await Navigation.ResetToAsync<StartupErrorViewModel>();
        }
    }
}
