using FieldReport.Core.Models;
using FieldReport.Core.Presentation;
using FieldReport.Tests.TestData;

namespace FieldReport.Tests.Presentation;

/// <summary>Formatter: data w kulturze lokalizatora (pl-PL), statusy i priorytety jako klucze zasobów.</summary>
[TestFixture]
public class ReportFormatterTests
{
    private ReportFormatter _formatter = null!;

    [SetUp]
    public void SetUp() => _formatter = new ReportFormatter(new KeyLocalizer()); // kultura pl-PL, tekst = klucz

    [Test]
    [Ignore("Ćwiczenie 06: usuń Ignore po uzupełnieniu TODO")]
    public void FormatDate_UsesPolishShortDateAndTime()
    {
        // TODO: data 26.09.2026 14:05 jako DateTimeOffset z lokalnym przesunięciem (TimeZoneInfo.Local.GetUtcOffset)
        // → FormatDate zwraca "26.09.2026 14:05"
        Assert.Fail("TODO");
    }

    [TestCase(ReportStatus.New, "ReportStatus_New")]
    [TestCase(ReportStatus.InProgress, "ReportStatus_InProgress")]
    [TestCase(ReportStatus.Done, "ReportStatus_Done")]
    public void FormatStatus_UsesStatusResourceKey(ReportStatus status, string expectedKey) =>
        Assert.That(_formatter.FormatStatus(status), Is.EqualTo(expectedKey));

    [TestCase(ReportPriority.Low, "ReportPriority_Low")]
    [TestCase(ReportPriority.Normal, "ReportPriority_Normal")]
    [TestCase(ReportPriority.High, "ReportPriority_High")]
    public void FormatPriority_UsesPriorityResourceKey(ReportPriority priority, string expectedKey) =>
        Assert.That(_formatter.FormatPriority(priority), Is.EqualTo(expectedKey));
}
