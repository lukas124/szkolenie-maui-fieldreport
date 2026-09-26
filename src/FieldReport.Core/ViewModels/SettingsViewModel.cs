using CommunityToolkit.Mvvm.Input;
using FieldReport.Core.Navigation;
using FieldReport.Core.ViewModels.Playground;

namespace FieldReport.Core.ViewModels;

/// <summary>Ustawienia. Na razie tylko narzędzia dewelopera (Playground).</summary>
public sealed partial class SettingsViewModel(INavigationService navigation) : BaseViewModel(navigation)
{
    // piaskownica do pokazów XAML
    [RelayCommand]
    private Task OpenPlaygroundAsync() => Navigation.NavigateToAsync<PlaygroundViewModel>();
}
