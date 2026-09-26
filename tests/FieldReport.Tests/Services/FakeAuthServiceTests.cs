using FieldReport.Core.Models;
using FieldReport.Core.Services;
using FieldReport.Tests.TestData;

namespace FieldReport.Tests.Services;

[TestFixture]
public class FakeAuthServiceTests
{
    private Mock<ISessionStore> _sessions = null!;
    private FakeAuthService _auth = null!;

    [SetUp]
    public void SetUp()
    {
        _sessions = new Mock<ISessionStore>();
        _auth = new FakeAuthService(_sessions.Object);
    }

    [Test]
    public async Task SignIn_TrainingPin_SavesSession_WithNameFromEmail()
    {
        var result = await _auth.SignInAsync("jan.kowalski@firma.pl", "1234");

        Assert.That(result.IsSuccess, Is.True);
        _sessions.Verify(s => s.SaveAsync(It.Is<Session>(x =>
            x.Email == "jan.kowalski@firma.pl" && x.TechnicianName == "Jan Kowalski" && x.AccessToken != "" && x.RefreshToken != "")), Times.Once);
    }

    [Test]
    public async Task SignIn_WrongPin_FailsWithInvalidCredentials_AndSavesNothing()
    {
        var result = await _auth.SignInAsync("jan@firma.pl", "4321");

        Assert.That(result.Error, Is.EqualTo(Errors.InvalidCredentials));
        _sessions.Verify(s => s.SaveAsync(It.IsAny<Session>()), Times.Never);
    }

    [Test]
    public async Task HasValidSession_DependsOnStoredSession()
    {
        Assert.That(await _auth.HasValidSessionAsync(), Is.False);
        _sessions.Setup(s => s.GetAsync()).ReturnsAsync(new Session("a@b.pl", "A", "t", "r"));
        Assert.That(await _auth.HasValidSessionAsync(), Is.True);
    }

    [Test]
    public async Task SignOut_ClearsSession()
    {
        await _auth.SignOutAsync();
        _sessions.Verify(s => s.ClearAsync(), Times.Once);
    }

    [TestCase("jan.kowalski@firma.pl", "Jan Kowalski")]
    [TestCase("trener.dev@example.com", "Trener Dev")]
    [TestCase("ANNA_NOWAK@x.pl", "Anna Nowak")]
    [TestCase("łukasz@x.pl", "Łukasz")]
    public void TechnicianName_FromEmail(string email, string expected) =>
        Assert.That(TechnicianNames.FromEmail(email), Is.EqualTo(expected));
}
