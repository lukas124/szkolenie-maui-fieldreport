namespace FieldReport.Core.Navigation;

/// <summary>DeepLink → nawigacja. Link z zimnego startu czeka do końca startu aplikacji i trafia tylko do zalogowanego.</summary>
public interface IDeepLinkRouter
{
    Task RouteAsync(DeepLink link);
    Task OnStartupCompletedAsync(bool isSignedIn);
}
