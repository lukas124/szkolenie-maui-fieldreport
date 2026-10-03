using FieldReport.Core.Navigation;

namespace FieldReport.App.Navigation;

/// <summary>Nawigacja na stosie stron (NavigationPage) bieżącego okna.</summary>
public sealed class NavigationService : INavigationService
{
    // Usługa nawigacji powstaje raz (singleton), a okno tworzy MAUI później, w App.CreateWindow — i może je
    // utworzyć ponownie (np. po zamknięciu aplikacji przyciskiem Wstecz i ponownym otwarciu). Dlatego nie
    // zapamiętujemy nawigacji w polu, tylko pobieramy ją z bieżącego okna przy każdym użyciu.
    private static INavigation CurrentNavigation =>
        Application.Current?.Windows.FirstOrDefault()?.Page?.Navigation
        ?? throw new InvalidOperationException("The window has no page.");

    /// <summary>Zamyka okno modalne, a gdy go nie ma — zdejmuje bieżącą stronę ze stosu.</summary>
    public async Task GoBackAsync()
    {
        if (CurrentNavigation.ModalStack.Count > 0) await CurrentNavigation.PopModalAsync(false);
        else if (CurrentNavigation.NavigationStack.Count > 1) await CurrentNavigation.PopAsync(true);
    }
}
