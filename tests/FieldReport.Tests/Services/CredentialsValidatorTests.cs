using FieldReport.Core.Services;

namespace FieldReport.Tests.Services;

[TestFixture]
public class CredentialsValidatorTests
{
    private readonly CredentialsValidator _validator = new();

    [TestCase("jan.kowalski@firma.pl", true)]
    [TestCase(" jan@firma.pl ", true)]
    [TestCase("trener.dev@example.com", true)]
    [TestCase("jan@firma", false)]
    [TestCase("jan firma@x.pl", false)]
    [TestCase("@firma.pl", false)]
    [TestCase("", false)]
    [TestCase(null, false)]
    public void Email(string? email, bool expected) => Assert.That(_validator.IsValid(email, "1234"), Is.EqualTo(expected));

    [TestCase("123", false)]
    [TestCase("1234", true)]
    [TestCase("123456", true)]
    [TestCase("1234567", false)]
    [TestCase("12a4", false)]
    [TestCase("١٢٣٤", false)] // cyfry spoza ASCII
    [TestCase(null, false)]
    public void Pin(string? pin, bool expected) => Assert.That(_validator.IsValid("jan@firma.pl", pin), Is.EqualTo(expected));
}
