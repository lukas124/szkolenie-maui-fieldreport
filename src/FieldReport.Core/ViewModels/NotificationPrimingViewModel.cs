using CommunityToolkit.Mvvm.Input;
using FieldReport.Core.Navigation;
using FieldReport.Core.Presentation;
using FieldReport.Core.Services.Push;

namespace FieldReport.Core.ViewModels;

/// <summary>Ekran „Włącz powiadomienia” (modal po pierwszym logowaniu) — tylko orkiestruje: zgoda → rejestracja → zamknięcie.</summary>
public sealed partial class NotificationPrimingViewModel(
    INavigationService navigation, IPushPermissionService permission,
    IDeviceRegistrationService devices, IUserFeedback feedback) : BaseViewModel(navigation)
{
    [RelayCommand]
    private async Task EnableAsync(CancellationToken ct)
    {
        // systemowe okno zgody
        if (await permission.RequestAsync() == PushPermissionStatus.Granted)
        {
            var result = await devices.RegisterAsync(ct); // POST api/devices
            if (!result.IsSuccess) await feedback.ShowErrorAsync(result.Error!);
        }
        await Navigation.GoBackAsync();               // zamyka modal
    }

    [RelayCommand]
    private Task NotNowAsync() => Navigation.GoBackAsync();
}
