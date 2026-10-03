using CommunityToolkit.Mvvm.Input;
using FieldReport.Core.Navigation;
using FieldReport.Core.Presentation;
using FieldReport.Core.Services.Push;

namespace FieldReport.Core.ViewModels;

/// <summary>Ekran „Włącz powiadomienia” (modal po pierwszym logowaniu) — tylko orkiestruje: zgoda → rejestracja → zamknięcie.</summary>
// TODO: dodaj parametry konstruktora (po navigation): IPushPermissionService permission, IDeviceRegistrationService devices,
// IUserFeedback feedback
public sealed partial class NotificationPrimingViewModel(INavigationService navigation) : BaseViewModel(navigation)
{
    [RelayCommand]
    private Task EnableAsync(CancellationToken ct)
    {
        // TODO: permission.RequestAsync(); zgoda (Granted) → devices.RegisterAsync(ct), błąd → feedback.ShowErrorAsync;
        // na końcu zawsze zamknij ekran
        return Navigation.GoBackAsync();
    }

    [RelayCommand]
    private Task NotNowAsync() => Navigation.GoBackAsync();
}
