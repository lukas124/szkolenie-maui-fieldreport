using FieldReport.Core.Models;
using FieldReport.Core.Presentation;
using FieldReport.Core.ViewModels.NewReport;
using FieldReport.Core.ValueObjects;
using FieldReport.Tests.TestData;

namespace FieldReport.Tests.ViewModels.NewReport;

[TestFixture]
public class SummaryStepViewModelTests
{
    [Test]
    public void OnShown_ShowsCurrentDraft_WithTextsFromFormatter()
    {
        var formatter = Mock.Of<IReportFormatter>(f => f.FormatPriority(ReportPriority.High) == "Wysoki");
        var step = new SummaryStepViewModel(formatter, new KeyLocalizer());
        var draft = new NewReportDraft { Title = ReportTitle.Create("Awaria pompy").Value, Description = " Buczy ", Priority = ReportPriority.High, PhotoPath = "/p.jpg" };
        step.Attach(draft);

        step.OnShown();

        Assert.Multiple(() =>
        {
            Assert.That(step.ReportTitle, Is.EqualTo("Awaria pompy"));
            Assert.That(step.DescriptionText, Is.EqualTo("Buczy"));
            Assert.That(step.PriorityText, Is.EqualTo("Wysoki"));
            Assert.That(step.PhotoPath, Is.EqualTo("/p.jpg"));
            Assert.That(step.IsValid, Is.True);
        });
    }

    [Test]
    public void OnShown_AgainAfterChanges_RefreshesTexts_AndEmptyDescriptionUsesResource()
    {
        var step = new SummaryStepViewModel(Mock.Of<IReportFormatter>(), new KeyLocalizer());
        var draft = new NewReportDraft { Title = ReportTitle.Create("Stary").Value };
        step.Attach(draft);
        step.OnShown();

        draft.Title = ReportTitle.Create("Nowy tytuł").Value;
        step.OnShown();

        Assert.That(step.ReportTitle, Is.EqualTo("Nowy tytuł"));
        Assert.That(step.DescriptionText, Is.EqualTo("Details_NoDescription"));
    }
}
