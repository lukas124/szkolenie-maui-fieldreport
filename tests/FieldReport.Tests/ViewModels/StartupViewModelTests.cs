using FieldReport.Core.Navigation;
using FieldReport.Core.Services;
using FieldReport.Core.ViewModels;

namespace FieldReport.Tests.ViewModels;

[TestFixture]
public class StartupViewModelTests
{
    private Mock<INavigationService> _navigation = null!;
    private Mock<IAuthService> _auth = null!;
    private Mock<IDeepLinkRouter> _deepLinks = null!;
    private StartupViewModel _vm = null!;

    [SetUp]
    public void SetUp()
    {
        _navigation = new Mock<INavigationService>(MockBehavior.Strict);
        _auth = new Mock<IAuthService>();
        _deepLinks = new Mock<IDeepLinkRouter>();
        _vm = new StartupViewModel(_navigation.Object, _auth.Object, _deepLinks.Object);
    }

    [Test]
    public async Task SignedIn_ResetsToList_ThenCompletesStartup()
    {
        var sequence = new MockSequence();
        _auth.Setup(a => a.HasValidSessionAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _navigation.InSequence(sequence).Setup(n => n.ResetToAsync<ReportsViewModel>()).Returns(Task.CompletedTask);
        _deepLinks.InSequence(sequence).Setup(d => d.OnStartupCompletedAsync(true)).Returns(Task.CompletedTask);

        await _vm.OnNavigatedToAsync(null);

        _deepLinks.Verify(d => d.OnStartupCompletedAsync(true), Times.Once);
    }

    [Test]
    public async Task NoSession_ResetsToLogin()
    {
        _auth.Setup(a => a.HasValidSessionAsync(It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _navigation.Setup(n => n.ResetToAsync<LoginViewModel>()).Returns(Task.CompletedTask);

        await _vm.OnNavigatedToAsync(null);

        _navigation.Verify(n => n.ResetToAsync<LoginViewModel>(), Times.Once);
        _deepLinks.Verify(d => d.OnStartupCompletedAsync(false), Times.Once);
    }

    [Test]
    public void SessionCheckThrows_ExceptionPropagates_ToStartupErrorHandling()
    {
        _auth.Setup(a => a.HasValidSessionAsync(It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException());
        Assert.That(() => _vm.OnNavigatedToAsync(null), Throws.InvalidOperationException);
    }
}

[TestFixture]
public class StartupErrorViewModelTests
{
    [Test]
    public async Task Retry_RestartsStartup()
    {
        var navigation = new Mock<INavigationService>();
        var vm = new StartupErrorViewModel(navigation.Object, Mock.Of<IErrorHandler>());

        await vm.RetryCommand.ExecuteAsync(null);

        navigation.Verify(n => n.ResetToAsync<StartupViewModel>(), Times.Once);
    }

    [Test]
    public async Task Retry_FailsAgain_ReturnsToErrorScreen_WithoutThrowing()
    {
        var navigation = new Mock<INavigationService>();
        var errors = new Mock<IErrorHandler>();
        navigation.Setup(n => n.ResetToAsync<StartupViewModel>()).ThrowsAsync(new IOException());
        var vm = new StartupErrorViewModel(navigation.Object, errors.Object);

        await vm.RetryCommand.ExecuteAsync(null);

        errors.Verify(e => e.Handle(It.IsAny<IOException>()), Times.Once);
        navigation.Verify(n => n.ResetToAsync<StartupErrorViewModel>(), Times.Once);
    }
}
