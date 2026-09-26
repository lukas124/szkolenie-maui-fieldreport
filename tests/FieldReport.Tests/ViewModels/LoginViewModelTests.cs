using FieldReport.Core.Models;
using FieldReport.Core.Navigation;
using FieldReport.Core.Presentation;
using FieldReport.Core.Services;
using FieldReport.Core.ViewModels;

namespace FieldReport.Tests.ViewModels;

[TestFixture]
public class LoginViewModelTests
{
    private Mock<INavigationService> _navigation = null!;
    private Mock<IAuthService> _auth = null!;
    private Mock<IUserFeedback> _feedback = null!;
    private LoginViewModel _vm = null!;

    [SetUp]
    public void SetUp()
    {
        _navigation = new Mock<INavigationService>();
        _auth = new Mock<IAuthService>();
        _feedback = new Mock<IUserFeedback>();
        _vm = new LoginViewModel(_navigation.Object, _auth.Object, new CredentialsValidator(), _feedback.Object);
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
}
