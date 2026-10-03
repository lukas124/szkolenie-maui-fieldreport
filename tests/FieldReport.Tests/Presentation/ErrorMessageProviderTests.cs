using FieldReport.Core.Models;
using FieldReport.Core.Presentation;
using FieldReport.Core.Services;

namespace FieldReport.Tests.Presentation;

[TestFixture]
public class ErrorMessageProviderTests
{
    private ErrorMessageProvider _provider = null!;

    [SetUp]
    public void SetUp()
    {
        var localizer = new Mock<ILocalizer>();
        localizer.Setup(l => l.GetString(It.IsAny<string>())).Returns((string key) => key); // brak tekstu → klucz
        localizer.Setup(l => l.GetString("Error_network")).Returns("Brak połączenia z internetem.");
        localizer.Setup(l => l.GetString("Error_unknown")).Returns("Wystąpił nieoczekiwany błąd.");
        _provider = new ErrorMessageProvider(localizer.Object);
    }

    [Test]
    public void GetMessage_KnownCode_ReturnsTextFromResources()
    {
        Assert.That(_provider.GetMessage(Errors.Network), Is.EqualTo("Brak połączenia z internetem."));
    }

    [Test]
    public void GetMessage_UnknownCode_FallsBackToGenericMessage()
    {
        Assert.That(_provider.GetMessage(new Error("something_new")), Is.EqualTo("Wystąpił nieoczekiwany błąd."));
    }
}
