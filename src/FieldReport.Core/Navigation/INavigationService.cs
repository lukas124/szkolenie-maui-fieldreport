namespace FieldReport.Core.Navigation;

/// <summary>
/// Nawigacja z poziomu ViewModelu (ViewModel-first): ViewModel mówi „pokaż szczegóły tego zgłoszenia”
/// podając typ docelowego ViewModelu, a nie stronę MAUI. Którą stronę pokazać, wie tylko App (PageRoutes).
/// </summary>
public interface INavigationService
{
    /// <summary>
    /// Wstawia na stos stronę dla <typeparamref name="TViewModel"/>.
    /// <paramref name="parameter"/> (np. Id zgłoszenia) trafia do
    /// <see cref="BaseViewModel.OnNavigatedToAsync"/> nowego ViewModelu.
    /// </summary>
    Task NavigateToAsync<TViewModel>(object? parameter = null) where TViewModel : BaseViewModel;

    /// <summary>Otwiera stronę jako okno modalne (na całą wysokość,
    /// nad stosem), np. kreator nowego zgłoszenia.</summary>
    Task OpenModalAsync<TViewModel>(object? parameter = null) where TViewModel : BaseViewModel;

    /// <summary>
    /// Zaczyna stos od nowa: strona dla <typeparamref name="TViewModel"/>
    /// zostaje jedyną stroną (np. lista po zalogowaniu), więc przycisk „wstecz”
    /// nie wróci już do poprzednich ekranów.
    /// </summary>
    Task ResetToAsync<TViewModel>() where TViewModel : BaseViewModel;

    /// <summary>Wraca do poprzedniej strony (albo zamyka okno modalne).</summary>
    Task GoBackAsync();
}
