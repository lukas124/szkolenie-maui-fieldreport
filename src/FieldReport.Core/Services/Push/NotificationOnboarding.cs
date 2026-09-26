namespace FieldReport.Core.Services.Push;

public sealed class NotificationOnboarding(IAppPreferences preferences, IDeviceRegistrationService devices) : INotificationOnboarding
{
    public async Task<bool> AfterSignInAsync(CancellationToken ct = default)
    {
        if (!preferences.NotificationPrimingShown)
        {
            preferences.NotificationPrimingShown = true;
            return true;
        }

        // ponowne logowanie (np. po wylogowaniu): zgoda już jest → urządzenie znów przypisujemy do technika
        await devices.RegisterIfPermittedAsync(ct);
        return false;
    }
}
