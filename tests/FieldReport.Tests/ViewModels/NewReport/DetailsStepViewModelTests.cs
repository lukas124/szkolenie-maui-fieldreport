using FieldReport.Core.Models;
using FieldReport.Core.Presentation;
using FieldReport.Core.ViewModels.NewReport;
using FieldReport.Tests.TestData;

namespace FieldReport.Tests.ViewModels.NewReport;

[TestFixture]
public class DetailsStepViewModelTests
{
    private NewReportDraft _draft = null!;
    private DetailsStepViewModel _step = null!;

    [SetUp]
    public void SetUp()
    {
        var messages = Mock.Of<IErrorMessageProvider>(m => m.GetMessage(Errors.InvalidTitle) == "Tytuł musi mieć od 3 do 80 znaków.");
        _step = new DetailsStepViewModel(messages, new KeyLocalizer());
        _draft = new NewReportDraft();
        _step.Attach(_draft);
    }

    [Test]
    public void TitleAb_IsInvalid_ShowsError_AndClearsDraftTitle()
    {
        _step.TitleInput = "ab";

        Assert.Multiple(() =>
        {
            Assert.That(_step.IsValid, Is.False);
            Assert.That(_step.TitleError, Is.EqualTo("Tytuł musi mieć od 3 do 80 znaków."));
            Assert.That(_draft.Title, Is.Null);
        });
    }

    [Test]
    public void ValidTitle_IsValid_AndWritesTrimmedTitleToDraft()
    {
        _step.TitleInput = "  Awaria pompy ";

        Assert.That(_step.IsValid, Is.True);
        Assert.That(_step.TitleError, Is.Null);
        Assert.That(_draft.Title!.Value, Is.EqualTo("Awaria pompy"));
    }

    [Test]
    public void EmptyTitle_IsInvalid_WithoutError()
    {
        _step.TitleInput = "Awaria";
        _step.TitleInput = "";

        Assert.That(_step.IsValid, Is.False);
        Assert.That(_step.TitleError, Is.Null); // pusty formularz nie krzyczy błędem
    }

    [Test]
    public void ValidityChanged_IsRaised_WhenValidityFlips()
    {
        var raised = 0;
        _step.ValidityChanged += (_, _) => raised++;

        _step.TitleInput = "Awaria";
        _step.TitleInput = "Awaria pompy"; // nadal poprawny: bez zdarzenia
        _step.TitleInput = "a";

        Assert.That(raised, Is.EqualTo(2));
    }

    [Test]
    public void Priority_DefaultsToNormal_AndIsWrittenToDraft()
    {
        Assert.That(_step.SelectedPriority!.Priority, Is.EqualTo(ReportPriority.Normal));

        _step.SelectedPriority = _step.Priorities.Single(p => p.Priority == ReportPriority.High);

        Assert.That(_draft.Priority, Is.EqualTo(ReportPriority.High));
    }

    [Test]
    public void Priority_SetToNull_ByView_KeepsDraftValue()
    {
        _step.SelectedPriority = _step.Priorities.Single(p => p.Priority == ReportPriority.Low);

        _step.SelectedPriority = null; // Picker po odtworzeniu widoku kroku

        Assert.That(_step.SelectedPriority!.Priority, Is.EqualTo(ReportPriority.Low));
        Assert.That(_draft.Priority, Is.EqualTo(ReportPriority.Low));
    }

    [Test]
    public void Description_IsWrittenToDraft() {
        _step.DescriptionInput = "Buczy silnik";
        Assert.That(_draft.Description, Is.EqualTo("Buczy silnik"));
    }
}
