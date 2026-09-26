using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FieldReport.Core.Models;
using FieldReport.Core.Navigation;
using FieldReport.Core.Presentation;
using FieldReport.Core.Services;
using FieldReport.Core.Services.Push;
using FieldReport.Core.ViewModels.Playground;

namespace FieldReport.Core.ViewModels;

/// <summary>Ustawienia: technik, środowisko, wersja, język, stan powiadomień + „Włącz”, (Debug) „Kopiuj token FCM”, Playground, „Wyloguj”.</summary>
public sealed partial class SettingsViewModel(
    INavigationService navigation,
    IAccountService account,
    IAppEnvironment environment,
    INotificationSettingsService notifications,
    ILocalizer localizer,
    IUserFeedback feedback) : BaseViewModel(navigation)
{
    [ObservableProperty]
    private string _technicianName = string.Empty;

    [ObservableProperty]
    private string _email = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AreNotificationsEnabled))]
    private PushPermissionStatus _notificationStatus;

    // Języki, dla których są pliki zasobów: Strings.resx (polski) i Strings.en.resx (angielski)
    public IReadOnlyList<LanguageOption> Languages { get; } = [new("pl-PL", "Polski"), new("en", "English")];

    [ObservableProperty]
    private LanguageOption? _selectedLanguage;

    public bool AreNotificationsEnabled => NotificationStatus == PushPermissionStatus.Granted;
    public string EnvironmentName => environment.EnvironmentName;
    public string VersionText => environment.VersionText;
    public bool IsDebug => environment.IsDebug;

    public override async Task OnNavigatedToAsync(object? parameter)
    {
        SelectedLanguage = Languages.FirstOrDefault(l => l.Code == localizer.CurrentCulture.Name) ?? Languages[0];

        var session = await account.GetSessionAsync();
        TechnicianName = session?.TechnicianName ?? string.Empty;
        Email = session?.Email ?? string.Empty;
        NotificationStatus = await notifications.GetStatusAsync();
    }

    // Powrót do aplikacji z ustawień systemu (Window.Resumed): zgoda mogła się zmienić
    public override async Task OnResumedAsync() => NotificationStatus = await notifications.GetStatusAsync();

    [RelayCommand]
    private async Task EnableNotificationsAsync(CancellationToken ct)
    {
        var result = await notifications.EnableAsync(ct);
        if (result.IsSuccess) NotificationStatus = result.Value;
        else await feedback.ShowErrorAsync(result.Error!);
    }

    [RelayCommand]
    private async Task CopyPushTokenAsync(CancellationToken ct)
    {
        if (!await notifications.CopyTokenAsync(ct)) { await feedback.ShowErrorAsync(Errors.PushTokenUnavailable); return; }

        await feedback.ShowInfoAsync("Settings_TokenCopiedTitle", "Settings_TokenCopiedMessage");
    }

    // Wybór w Pickerze → zmiana języka w locie (teksty z {loc:Translate} odświeżają się same)
    partial void OnSelectedLanguageChanged(LanguageOption? value)
    {
        if (value is not null && value.Code != localizer.CurrentCulture.Name)
            localizer.SetLocale(CultureInfo.GetCultureInfo(value.Code));
    }

    // piaskownica do pokazów XAML
    [RelayCommand]
    private Task OpenPlaygroundAsync() => Navigation.NavigateToAsync<PlaygroundViewModel>();

    [RelayCommand]
    private async Task SignOutAsync(CancellationToken ct)
    {
        // ostrzeżenie o niewysłanych: po wylogowaniu lokalne dane technika są usuwane z telefonu
        var pending = await account.CountPendingAsync(ct);
        var message = pending > 0 ? "Settings_SignOutPendingMessage" : "Settings_SignOutMessage";
        if (!await feedback.ConfirmAsync("Settings_SignOutTitle", message))
            return;

        await account.SignOutAsync(ct);
        await Navigation.ResetToAsync<LoginViewModel>();
    }
}
