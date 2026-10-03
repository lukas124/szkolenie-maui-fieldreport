using FieldReport.Core.Navigation;

namespace FieldReport.App.Navigation;

/// <summary>
/// Nawigacja ViewModel-first na stosie stron okna (RootPage : NavigationPage).
/// Strona i ViewModel pochodzą z kontenera DI, a PageRoutes mówi, która strona należy do którego ViewModelu.
/// Stos zmieniamy zawsze na wątku UI (MainThread): nawigację może wywołać kod działający w tle,
/// np. obsługa wygasłej sesji albo tapnięcie powiadomienia push.
/// </summary>
public sealed class NavigationService(IServiceProvider services, PageRoutes routes) : INavigationService
{
    // true = trwa zmiana stosu; kolejne wywołanie (np. drugie szybkie tapnięcie) jest wtedy ignorowane
    private bool _isNavigating;

    // Usługa nawigacji powstaje raz (singleton), a okno tworzy MAUI później, w App.CreateWindow — i może je
    // utworzyć ponownie (np. po zamknięciu aplikacji przyciskiem Wstecz i ponownym otwarciu). Dlatego nie
    // zapamiętujemy nawigacji w polu, tylko pobieramy ją z bieżącego okna przy każdym użyciu.
    private static INavigation CurrentNavigation =>
        Application.Current?.Windows.FirstOrDefault()?.Page?.Navigation
        ?? throw new InvalidOperationException("The window has no page.");

    /// <summary>Tworzy stronę i ViewModel, wstawia stronę na stos, a potem przekazuje parametr do ViewModelu.</summary>
    public async Task NavigateToAsync<TViewModel>(object? parameter = null) where TViewModel : BaseViewModel
    {
        if (_isNavigating) return;          // trwa inna nawigacja → ignoruj (np. drugie szybkie tapnięcie)
        _isNavigating = true;
        BaseViewModel viewModel;
        try
        {
            viewModel = CreatePage<TViewModel>(out var page); // 1) strona + ViewModel z DI (mapa PageRoutes)
            await MainThread.InvokeOnMainThreadAsync(() => CurrentNavigation.PushAsync(page)); // 2) połóż stronę na stosie
        }
        finally
        {
            _isNavigating = false; // 3) zwalniamy blokadę od razu po zmianie stosu, nie czekamy na dane
        }

        await LoadAsync(viewModel, parameter);  // 4) IsBusy = true → OnNavigatedToAsync(parameter) → IsBusy = false
    }

    /// <summary>Jak NavigateToAsync, ale strona trafia na stos modalny (zamyka ją GoBackAsync).</summary>
    public async Task OpenModalAsync<TViewModel>(object? parameter = null) where TViewModel : BaseViewModel
    {
        if (_isNavigating) return;
        _isNavigating = true;
        BaseViewModel viewModel;
        try
        {
            viewModel = CreatePage<TViewModel>(out var page);
            await MainThread.InvokeOnMainThreadAsync(() => CurrentNavigation.PushModalAsync(page));
        }
        finally
        {
            _isNavigating = false;
        }

        await LoadAsync(viewModel, parameter);
    }

    /// <summary>Zamyka okna modalne, wstawia nową stronę i usuwa ze stosu wszystkie strony pod nią.</summary>
    public async Task ResetToAsync<TViewModel>() where TViewModel : BaseViewModel
    {
        if (_isNavigating) return;
        _isNavigating = true;
        BaseViewModel viewModel;
        try
        {
            viewModel = CreatePage<TViewModel>(out var page);
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                while (CurrentNavigation.ModalStack.Count > 0)    // 1. zamknij modale
                    await CurrentNavigation.PopModalAsync(false);

                await CurrentNavigation.PushAsync(page, false);   // 2. nowa strona
                // Stare strony usuwamy dopiero PO dodaniu nowej, żeby stos ani na chwilę nie był pusty
                foreach (var oldPage in CurrentNavigation.NavigationStack.Where(p => p != page).ToList())
                    CurrentNavigation.RemovePage(oldPage);      // 3. usuń stare (PO dodaniu nowej)
            });
        }
        finally
        {
            _isNavigating = false;
        }

        await LoadAsync(viewModel, null);
    }

    /// <summary>Zamyka okno modalne, a gdy go nie ma — zdejmuje bieżącą stronę ze stosu.</summary>
    public async Task GoBackAsync()
    {
        if (_isNavigating) return;
        _isNavigating = true;
        try
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                if (CurrentNavigation.ModalStack.Count > 0)
                    await CurrentNavigation.PopModalAsync();   // otwarty modal – zamknij go
                else if (CurrentNavigation.NavigationStack.Count > 1)
                    await CurrentNavigation.PopAsync();        // poprzednia strona
            });
        }
        finally
        {
            _isNavigating = false;
        }
    }

    // Strona i ViewModel z kontenera DI; ViewModel staje się BindingContext strony
    private BaseViewModel CreatePage<TViewModel>(out Page page) where TViewModel : BaseViewModel
    {
        page = (Page)services.GetRequiredService(routes.GetPageType(typeof(TViewModel)));
        var viewModel = services.GetRequiredService<TViewModel>();
        page.BindingContext = viewModel;
        return viewModel;
    }

    // Strona jest już widoczna; dane ładują się pod wskaźnikiem ładowania (IsBusy)
    private static async Task LoadAsync(BaseViewModel viewModel, object? parameter)
    {
        viewModel.IsBusy = true;
        try
        {
            await viewModel.OnNavigatedToAsync(parameter);
        }
        finally
        {
            viewModel.IsBusy = false;
        }
    }
}
