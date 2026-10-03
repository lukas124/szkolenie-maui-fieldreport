using FieldReport.Core.Models;
using FieldReport.Core.Navigation;
using FieldReport.Core.Presentation;
using FieldReport.Core.Services;
using FieldReport.Core.ViewModels;
using FieldReport.Core.ViewModels.NewReport;

namespace FieldReport.Tests.ViewModels.NewReport;

[TestFixture]
public class NewReportFlowViewModelTests
{
    private Mock<INavigationService> _navigation = null!;
    private Mock<IReportService> _reports = null!;
    private Mock<IUserFeedback> _feedback = null!;
    private DetailsStepViewModel _details = null!;
    private NewReportFlowViewModel _vm = null!;

    [SetUp]
    public void SetUp()
    {
        _navigation = new Mock<INavigationService>();
        _reports = new Mock<IReportService>();
        _feedback = new Mock<IUserFeedback>();
        _details = new DetailsStepViewModel(Mock.Of<IErrorMessageProvider>(), Mock.Of<ILocalizer>());
        _vm = new NewReportFlowViewModel(_navigation.Object, _reports.Object, _feedback.Object,
            _details, new PhotoStepViewModel(Mock.Of<IMediaService>(), Mock.Of<IPermissionService>(), Mock.Of<IUserFeedback>(), Mock.Of<ILocalizer>()),
            new SummaryStepViewModel(Mock.Of<IReportFormatter>(), Mock.Of<ILocalizer>()));
    }

    [Test]
    public void StartsOnDetails_WithFirstProgress()
    {
        Assert.That(_vm.CurrentStepHost, Is.EqualTo(new[] { _details }));
        Assert.That(_vm.StepNumber, Is.EqualTo(1));
        Assert.That(_vm.IsFirstStep, Is.True);
    }

    [Test]
    public void Next_IsDisabled_UntilDetailsAreValid()
    {
        Assert.That(_vm.NextCommand.CanExecute(null), Is.False);
        _details.TitleInput = "Awaria pompy";
        Assert.That(_vm.NextCommand.CanExecute(null), Is.True);
    }

    [Test]
    public void Next_IsDisabled_ForTitleAb()
    {
        _details.TitleInput = "ab";
        Assert.That(_vm.NextCommand.CanExecute(null), Is.False);
    }

    [Test]
    public async Task Next_OnLastStep_SavesAndResetsToList()
    {
        _reports.Setup(r => r.CreateAsync(It.IsAny<NewReportDraft>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Guid>.Success(Guid.NewGuid()));
        _details.TitleInput = "Awaria pompy";
        await _vm.NextCommand.ExecuteAsync(null); // → Zdjęcie
        await _vm.NextCommand.ExecuteAsync(null); // → Podsumowanie

        await _vm.NextCommand.ExecuteAsync(null); // Zapisz

        _reports.Verify(r => r.CreateAsync(It.Is<NewReportDraft>(d => d.Title!.Value == "Awaria pompy"), It.IsAny<CancellationToken>()), Times.Once);
        _navigation.Verify(n => n.ResetToAsync<ReportsViewModel>(), Times.Once);
    }

    [Test]
    public async Task Next_SaveFails_ShowsError_AndStaysInWizard()
    {
        _reports.Setup(r => r.CreateAsync(It.IsAny<NewReportDraft>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Guid>.Failure(Errors.Unknown));
        _details.TitleInput = "Awaria pompy";
        await _vm.NextCommand.ExecuteAsync(null);
        await _vm.NextCommand.ExecuteAsync(null);

        await _vm.NextCommand.ExecuteAsync(null);

        _feedback.Verify(f => f.ShowErrorAsync(Errors.Unknown), Times.Once);
        _navigation.Verify(n => n.ResetToAsync<ReportsViewModel>(), Times.Never);
        Assert.That(_vm.IsLastStep, Is.True);
    }

    [Test]
    public async Task Back_OnSecondStep_ReturnsToDetails_WithoutQuestion()
    {
        _details.TitleInput = "Awaria pompy";
        await _vm.NextCommand.ExecuteAsync(null);

        await _vm.OnBackRequestedAsync();

        Assert.That(_vm.CurrentStepHost.Single(), Is.SameAs(_details));
        _feedback.Verify(f => f.ConfirmAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task Back_OnFirstStep_AsksToDiscard_AndClosesOnYes()
    {
        _feedback.Setup(f => f.ConfirmAsync("NewReport_ExitTitle", "NewReport_ExitMessage")).ReturnsAsync(true);

        await _vm.BackCommand.ExecuteAsync(null);

        _navigation.Verify(n => n.GoBackAsync(), Times.Once);
    }

    [Test]
    public async Task Back_OnFirstStep_No_StaysInWizard()
    {
        _feedback.Setup(f => f.ConfirmAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(false);

        await _vm.OnBackRequestedAsync();

        _navigation.Verify(n => n.GoBackAsync(), Times.Never);
    }

    [Test]
    public async Task Close_OnSecondStep_AsksToDiscard_AndClosesOnYes()
    {
        _details.TitleInput = "Awaria pompy";
        await _vm.NextCommand.ExecuteAsync(null);
        _feedback.Setup(f => f.ConfirmAsync("NewReport_ExitTitle", "NewReport_ExitMessage")).ReturnsAsync(true);

        await _vm.OnCloseRequestedAsync();

        _navigation.Verify(n => n.GoBackAsync(), Times.Once);
    }

    [Test]
    public async Task Save_DoubleTap_SavesOnlyOnce() // AsyncRelayCommand: CanExecute = false w trakcie wykonania
    {
        var save = new TaskCompletionSource<Result<Guid>>();
        _reports.Setup(r => r.CreateAsync(It.IsAny<NewReportDraft>(), It.IsAny<CancellationToken>())).Returns(save.Task);
        _details.TitleInput = "Awaria pompy";
        await _vm.NextCommand.ExecuteAsync(null);
        await _vm.NextCommand.ExecuteAsync(null);

        var first = _vm.NextCommand.ExecuteAsync(null);
        Assert.That(_vm.NextCommand.CanExecute(null), Is.False); // trwa zapis — przycisk nieaktywny
        if (_vm.NextCommand.CanExecute(null)) _vm.NextCommand.Execute(null); // drugie tapnięcie: Button MAUI sprawdza CanExecute
        save.SetResult(Result<Guid>.Success(Guid.NewGuid()));
        await first;

        _reports.Verify(r => r.CreateAsync(It.IsAny<NewReportDraft>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task Progress_FollowsSteps()
    {
        _details.TitleInput = "Awaria pompy";
        await _vm.NextCommand.ExecuteAsync(null);

        Assert.That(_vm.Progress, Is.EqualTo(2d / 3).Within(1e-9));
        Assert.That(_vm.StepNumber, Is.EqualTo(2));
    }
}
