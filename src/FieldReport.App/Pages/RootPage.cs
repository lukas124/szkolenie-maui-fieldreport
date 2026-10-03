using FieldReport.Core.ViewModels;

namespace FieldReport.App.Pages;

/// <summary>
/// Korzeń okna: NavigationPage, czyli stos stron (systemowy pasek ukryty — nagłówek rysuje PageTemplate z Templates/PageTemplate.xaml).
/// Rejestracja transient: każde okno ma własny korzeń.
/// Pierwsza strona to StartupPage (splash przy starcie aplikacji). StartupViewModel zastępuje ją (ResetToAsync) logowaniem albo listą,
/// więc strona startowa nie zostaje na stosie.
/// </summary>
public sealed class RootPage : NavigationPage
{
    private readonly StartupViewModel _startup;

    public RootPage(StartupPage startPage, StartupViewModel startup) : base(startPage)
    {
        _startup = startup;
        startPage.BindingContext = startup;
    }

#if IOS
    // iOS: przy ukrytym systemowym pasku nawigacji UIKit blokuje gest „przesuń, by wrócić” — przywracamy go własnym delegatem
    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();
        if ((Handler as IPlatformViewHandler)?.ViewController is not UIKit.UINavigationController navigation) return;

        navigation.InteractivePopGestureRecognizer!.Delegate = new SwipeBackGesture(navigation); // od lewej krawędzi ekranu
        if (OperatingSystem.IsIOSVersionAtLeast(26))
            navigation.InteractiveContentPopGestureRecognizer!.Delegate = new SwipeBackGesture(navigation); // iOS 26: z dowolnego miejsca
    }
#endif

    /// <summary>Start aplikacji: uruchamia decyzję startową (StartupViewModel.OnNavigatedToAsync). Wołane z Window.Created.</summary>
    public async Task InitializeAsync()
    {
        _startup.IsBusy = true;
        try
        {
            await _startup.OnNavigatedToAsync(null);
        }
        finally
        {
            _startup.IsBusy = false;
        }
    }
}
