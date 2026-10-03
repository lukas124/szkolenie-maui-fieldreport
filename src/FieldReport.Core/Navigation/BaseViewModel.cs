using CommunityToolkit.Mvvm.ComponentModel;

namespace FieldReport.Core.Navigation;

/// <summary>
/// Bazowy ViewModel strony: stan wspólny dla wszystkich ekranów (IsBusy, Title) i metody cyklu życia,
/// które BasePage wywołuje, gdy strona się pojawia, znika albo użytkownik naciska „wstecz”.
/// </summary>
public abstract partial class BaseViewModel : ObservableObject
{
    protected BaseViewModel(INavigationService navigation) => Navigation = navigation;

    protected INavigationService Navigation { get; } // przejścia między ekranami

    [ObservableProperty]
    private bool _isBusy;                    // true = pokaż wskaźnik ładowania

    [ObservableProperty]
    private string _title = string.Empty;    // tytuł strony

    // Metody cyklu życia – woła je strona (BasePage) albo NavigationService:
    /// <summary>Raz, po wejściu na stronę: tu ładujemy dane. <paramref name="parameter"/> = to, co przekazał nawigujący (albo null).</summary>
    public virtual Task OnNavigatedToAsync(object? parameter) => Task.CompletedTask; // RAZ, po wejściu

    /// <summary>Za każdym razem, gdy strona staje się widoczna (także po powrocie z innej strony).</summary>
    public virtual Task OnAppearingAsync() => Task.CompletedTask;    // za każdym pokazaniem strony

    /// <summary>Gdy strona przestaje być widoczna.</summary>
    public virtual Task OnDisappearingAsync() => Task.CompletedTask; // gdy strona znika

    /// <summary>Systemowy przycisk „wstecz”. Domyślnie: powrót do poprzedniej strony.</summary>
    public virtual Task OnBackRequestedAsync() => Navigation.GoBackAsync(); // „wstecz”

    /// <summary>„X” w nagłówku strony (ShowCloseButton). Domyślnie: zamknięcie strony.</summary>
    public virtual Task OnCloseRequestedAsync() => Navigation.GoBackAsync();
}
