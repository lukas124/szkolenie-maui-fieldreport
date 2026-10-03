using FieldReport.Core.Models;
using FieldReport.Core.Navigation;
using FieldReport.Core.Presentation;
using FieldReport.Core.Services;
using FieldReport.Core.Services.Push;
using FieldReport.Core.ViewModels;

namespace FieldReport.Tests.ViewModels;

[TestFixture]
public class LoginViewModelTests
{
    private Mock<INavigationService> _navigation = null!;
    private Mock<IAuthService> _auth = null!;
    private Mock<IUserFeedback> _feedback = null!;
    private Mock<INotificationOnboarding> _onboarding = null!;
    private LoginViewModel _vm = null!;

    [SetUp]
    public void SetUp()
    {
        _navigation = new Mock<INavigationService>();
        _auth = new Mock<IAuthService>();
        _feedback = new Mock<IUserFeedback>();
        _onboarding = new Mock<INotificationOnboarding>();
        _vm = new LoginViewModel(_navigation.Object, _auth.Object, new CredentialsValidator(), _feedback.Object, _onboarding.Object);
    }

    [TestCase("", "")]
    [TestCase("jan.kowalski", "1234")]
    [TestCase("jan@firma.pl", "12")]
    [TestCase("jan@firma.pl", "12ab")]
    public void SignIn_IsDisabled_ForInvalidCredentials(string email, string pin)
    {
        _vm.Email = email;
        _vm.Pin = pin;
        Assert.That(_vm.SignInCommand.CanExecute(null), Is.False);
    }

    [Test]
    public void SignIn_IsEnabled_ForValidCredentials()
    {
        _vm.Email = "jan@firma.pl";
        _vm.Pin = "1234";
        Assert.That(_vm.SignInCommand.CanExecute(null), Is.True);
    }

    [Test]
    public async Task SignIn_Success_ResetsToList()
    {
        _auth.Setup(a => a.SignInAsync("jan@firma.pl", "1234", It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success());
        _vm.Email = " jan@firma.pl ";
        _vm.Pin = "1234";

        await _vm.SignInCommand.ExecuteAsync(null);

        _navigation.Verify(n => n.ResetToAsync<ReportsViewModel>(), Times.Once);
        Assert.That(_vm.IsBusy, Is.False);
    }

    [Test]
    public async Task SignIn_Failure_ShowsError_AndStaysOnLogin()
    {
        _auth.Setup(a => a.SignInAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync(Result.Failure(Errors.InvalidCredentials));
        _vm.Email = "jan@firma.pl";
        _vm.Pin = "9999";

        await _vm.SignInCommand.ExecuteAsync(null);

        _feedback.Verify(f => f.ShowErrorAsync(Errors.InvalidCredentials), Times.Once);
        _navigation.Verify(n => n.ResetToAsync<ReportsViewModel>(), Times.Never);
        Assert.That(_vm.IsBusy, Is.False);
    }

    [Test]
    public async Task SignIn_DoubleTap_SignsInOnce()
    {
        var signIn = new TaskCompletionSource<Result>();
        _auth.Setup(a => a.SignInAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(signIn.Task);
        _vm.Email = "jan@firma.pl";
        _vm.Pin = "1234";

        var first = _vm.SignInCommand.ExecuteAsync(null);
        Assert.That(_vm.SignInCommand.CanExecute(null), Is.False);
        if (_vm.SignInCommand.CanExecute(null)) _vm.SignInCommand.Execute(null); // drugie tapnięcie jak w Button MAUI
        signIn.SetResult(Result.Success());
        await first;

        _auth.Verify(a => a.SignInAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    // ----- Powiadomienia push: ekran „Włącz powiadomienia” tylko po pierwszym logowaniu -----

    private async Task SignInSuccessfullyAsync()
    {
        _auth.Setup(a => a.SignInAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success());
        _vm.Email = "jan@firma.pl";
        _vm.Pin = "1234";
        await _vm.SignInCommand.ExecuteAsync(null);
    }

    [Test]
    public async Task FirstSignIn_ResetsToList_ThenShowsNotificationPriming()
    {
        var sequence = new MockSequence();
        _navigation.InSequence(sequence).Setup(n => n.ResetToAsync<ReportsViewModel>()).Returns(Task.CompletedTask);
        _navigation.InSequence(sequence).Setup(n => n.OpenModalAsync<NotificationPrimingViewModel>(null)).Returns(Task.CompletedTask);
        _onboarding.Setup(o => o.AfterSignInAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);

        await SignInSuccessfullyAsync();

        _navigation.Verify(n => n.OpenModalAsync<NotificationPrimingViewModel>(null), Times.Once);
    }

    [Test]
    public async Task LaterSignIn_DoesNotShowPriming()
    {
        _onboarding.Setup(o => o.AfterSignInAsync(It.IsAny<CancellationToken>())).ReturnsAsync(false);

        await SignInSuccessfullyAsync();

        _navigation.Verify(n => n.OpenModalAsync<NotificationPrimingViewModel>(It.IsAny<object?>()), Times.Never);
    }

    [Test]
    public async Task FailedSignIn_DoesNotTouchOnboarding()
    {
        _auth.Setup(a => a.SignInAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync(Result.Failure(Errors.Network));
        _vm.Email = "jan@firma.pl";
        _vm.Pin = "1234";

        await _vm.SignInCommand.ExecuteAsync(null);

        _feedback.Verify(f => f.ShowErrorAsync(Errors.Network), Times.Once);
        _onboarding.Verify(o => o.AfterSignInAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
