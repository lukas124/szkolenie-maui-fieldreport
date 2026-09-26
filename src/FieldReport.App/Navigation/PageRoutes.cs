namespace FieldReport.App.Navigation;

/// <summary>
/// Mapa „ViewModel → strona”. NavigationService dostaje typ ViewModelu i tu sprawdza, którą stronę pokazać.
/// Mapę wypełnia MauiProgram: <c>routes.Map&lt;ReportsViewModel, ReportsPage&gt;();</c>
/// </summary>
public sealed class PageRoutes
{
    private readonly Dictionary<Type, Type> _routes = new(); // klucz: typ ViewModelu, wartość: typ strony

    /// <summary>Dodaje parę: ViewModel i strona, która go wyświetla.</summary>
    public void Map<TViewModel, TPage>() => _routes[typeof(TViewModel)] = typeof(TPage);

    /// <summary>Typ strony dla podanego ViewModelu (brak wpisu = błąd programisty, widoczny od razu).</summary>
    public Type GetPageType(Type viewModelType) => _routes[viewModelType];
}
