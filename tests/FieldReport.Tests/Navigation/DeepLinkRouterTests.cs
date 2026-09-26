using FieldReport.Core.Navigation;
using FieldReport.Core.ViewModels;

namespace FieldReport.Tests.Navigation;

[TestFixture]
public class DeepLinkRouterTests
{
    private Mock<INavigationService> _navigation = null!;
    private DeepLinkRouter _router = null!;
    private readonly Guid _reportId = Guid.NewGuid();

    [SetUp]
    public void SetUp()
    {
        _navigation = new Mock<INavigationService>();
        _router = new DeepLinkRouter(_navigation.Object);
    }

    private void VerifyDetails(Times times) =>
        _navigation.Verify(n => n.NavigateToAsync<ReportDetailsViewModel>(_reportId), times);

    [Test]
    public async Task LinkBeforeStartup_Waits_ThenOpensDetailsForSignedIn()
    {
        await _router.RouteAsync(new DeepLink(_reportId));
        VerifyDetails(Times.Never());

        await _router.OnStartupCompletedAsync(isSignedIn: true);
        VerifyDetails(Times.Once());
    }

    [Test]
    public async Task LinkBeforeStartup_NotSignedIn_IsDropped()
    {
        await _router.RouteAsync(new DeepLink(_reportId));
        await _router.OnStartupCompletedAsync(isSignedIn: false);
        VerifyDetails(Times.Never());
    }

    [Test]
    public async Task LinkAfterStartup_NavigatesImmediately()
    {
        await _router.OnStartupCompletedAsync(true);
        await _router.RouteAsync(new DeepLink(_reportId));
        VerifyDetails(Times.Once());
    }

    [Test]
    public async Task PendingLink_IsUsedOnlyOnce()
    {
        await _router.RouteAsync(new DeepLink(_reportId));
        await _router.OnStartupCompletedAsync(true);
        await _router.OnStartupCompletedAsync(true); // np. ponowny start okna
        VerifyDetails(Times.Once());
    }

    [Test]
    public async Task TwoLinksBeforeStartup_OnlyLastIsOpened()
    {
        var first = Guid.NewGuid();
        await _router.RouteAsync(new DeepLink(first));
        await _router.RouteAsync(new DeepLink(_reportId));
        await _router.OnStartupCompletedAsync(true);

        VerifyDetails(Times.Once());
        _navigation.Verify(n => n.NavigateToAsync<ReportDetailsViewModel>(first), Times.Never);
    }
}
