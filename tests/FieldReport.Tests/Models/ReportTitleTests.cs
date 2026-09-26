using FieldReport.Core.Models;
using FieldReport.Core.ValueObjects;

namespace FieldReport.Tests.Models;

[TestFixture]
public class ReportTitleTests
{
    [TestCase(2, false)]
    [TestCase(3, true)]
    [TestCase(80, true)]
    [TestCase(81, false)]
    public void Create_ChecksLength(int length, bool expected) =>
        Assert.That(ReportTitle.Create(new string('a', length)).IsSuccess, Is.EqualTo(expected));

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void Create_Empty_FailsWithInvalidTitle(string? raw) =>
        Assert.That(ReportTitle.Create(raw).Error, Is.EqualTo(Errors.InvalidTitle));

    [Test]
    public void Create_TrimsWhitespace_BeforeCheckingLength()
    {
        Assert.That(ReportTitle.Create("  ab  ").IsSuccess, Is.False);           // po przycięciu 2 znaki
        Assert.That(ReportTitle.Create("  Awaria  ").Value!.Value, Is.EqualTo("Awaria"));
    }
}
