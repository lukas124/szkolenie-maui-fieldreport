using FieldReport.Core.ViewModels;

namespace FieldReport.App.Pages;

/// <summary>
/// Korzeń okna: NavigationPage, czyli stos stron (systemowy pasek ukryty — nagłówek rysuje PageTemplate z Templates/PageTemplate.xaml).
/// Pierwsza strona to lista zgłoszeń.
/// Rejestracja transient: każde nowe okno dostaje własny korzeń.
/// </summary>
public sealed class RootPage : NavigationPage
{
    private readonly ReportsViewModel _startViewModel;

    public RootPage(ReportsPage startPage, ReportsViewModel startViewModel) : base(startPage)
    {
        _startViewModel = startViewModel;
        startPage.BindingContext = startViewModel;
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

    /// <summary>Jednorazowe ładowanie pierwszej strony (dla kolejnych stron robi to NavigationService). Wołane z Window.Created.</summary>
    public async Task InitializeAsync()
    {
        _startViewModel.IsBusy = true;
        try
        {
            await _startViewModel.OnNavigatedToAsync(null);
        }
        finally
        {
            _startViewModel.IsBusy = false;
        }
    }
}
