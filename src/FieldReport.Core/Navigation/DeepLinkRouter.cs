using FieldReport.Core.ViewModels;

namespace FieldReport.Core.Navigation;

/// <summary>
/// Deep link → nawigacja (singleton). Zanim start się skończy, link tylko zapamiętujemy:
/// nawigacja w trakcie routingu startowego zostałaby i tak usunięta resetem stosu.
/// </summary>
public sealed class DeepLinkRouter(INavigationService navigation) : IDeepLinkRouter
{
    private DeepLink? _pending;      // link odłożony na koniec startu
    private bool _startupCompleted;  // czy start już się skończył

    public Task RouteAsync(DeepLink link)
    {
        if (_startupCompleted) return NavigateAsync(link);
        _pending = link;
        return Task.CompletedTask;
    }

    // StartupViewModel: await deepLinks.OnStartupCompletedAsync(signedIn);
    public Task OnStartupCompletedAsync(bool isSignedIn)
    {
        _startupCompleted = true;        // od teraz RouteAsync nawiguje od razu
        var link = _pending;             // link odłożony w trakcie startu (albo null)
        _pending = null;
        return link is not null && isSignedIn ? NavigateAsync(link) : Task.CompletedTask;
    }

    private Task NavigateAsync(DeepLink link) => navigation.NavigateToAsync<ReportDetailsViewModel>(link.ReportId);
}
