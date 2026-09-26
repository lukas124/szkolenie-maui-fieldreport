namespace FieldReport.Core.Navigation;

/// <summary>
/// Nawigacja z poziomu ViewModelu: ViewModel mówi „wróć”, nie zna stron MAUI.
/// </summary>
public interface INavigationService
{
    /// <summary>Wraca do poprzedniej strony (albo zamyka okno modalne).</summary>
    Task GoBackAsync();
}
