using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FieldReport.Core.Navigation;
using FieldReport.Core.Presentation;
using FieldReport.Core.Services;
using FieldReport.Core.ViewModels.Playground;

namespace FieldReport.Core.ViewModels;

/// <summary>Ustawienia: technik, środowisko, wersja, język aplikacji (PL/EN), Playground i wylogowanie.</summary>
public sealed partial class SettingsViewModel(
    INavigationService navigation,
    IAuthService auth,
    IAppEnvironment environment,
    ILocalizer localizer,
    IUserFeedback feedback) : BaseViewModel(navigation)
{
    [ObservableProperty]
    private string _technicianName = string.Empty;

    [ObservableProperty]
    private string _email = string.Empty;

    // Języki, dla których są pliki zasobów: Strings.resx (polski) i Strings.en.resx (angielski)
    public IReadOnlyList<LanguageOption> Languages { get; } = [new("pl-PL", "Polski"), new("en", "English")];

    [ObservableProperty]
    private LanguageOption? _selectedLanguage;

    public string EnvironmentName => environment.EnvironmentName;
    public string VersionText => environment.VersionText;

    public override async Task OnNavigatedToAsync(object? parameter)
    {
        SelectedLanguage = Languages.FirstOrDefault(l => l.Code == localizer.CurrentCulture.Name) ?? Languages[0];

        var session = await auth.GetSessionAsync();
        TechnicianName = session?.TechnicianName ?? string.Empty;
        Email = session?.Email ?? string.Empty;
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
        if (!await feedback.ConfirmAsync("Settings_SignOutTitle", "Settings_SignOutMessage"))
            return;

        await auth.SignOutAsync(ct);
        await Navigation.ResetToAsync<LoginViewModel>();
    }
}
