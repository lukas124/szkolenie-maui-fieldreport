using CommunityToolkit.Mvvm.Messaging;
using FieldReport.Core.Models;
using FieldReport.Core.Navigation;
using FieldReport.Core.Presentation;
using FieldReport.Core.Services;
using FieldReport.Core.Services.Push;
using FieldReport.Core.ViewModels;
using FieldReport.Tests.TestData;

namespace FieldReport.Tests.ViewModels;

[TestFixture]
public class ReportsViewModelTests
{
    private Mock<INavigationService> _navigation = null!;
    private Mock<IReportService> _reports = null!;
    private Mock<IUserFeedback> _feedback = null!;
    private IMessenger _messenger = null!;
    private ReportsViewModel _vm = null!;

    [SetUp]
    public void SetUp()
    {
        _navigation = new Mock<INavigationService>();
        _reports = new Mock<IReportService>();
        _feedback = new Mock<IUserFeedback>();
        var items = new Mock<IReportItemFactory>();
        items.Setup(f => f.Create(It.IsAny<Report>())).Returns((Report r) => new ReportItem(
            r.Id, r.Title.Value, "data", r.Priority, "priorytet", r.Status, "status", r.SyncState == SyncState.Pending, null, r.CanDelete));

        _messenger = new WeakReferenceMessenger(); // własna instancja: testy nie dzielą odbiorców
        _vm = new ReportsViewModel(_navigation.Object, _reports.Object, items.Object, _feedback.Object, _messenger);
    }

    [Test]
    public async Task OnNavigatedTo_ServiceSucceeds_ShowsReports()
    {
        _reports.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<IReadOnlyList<Report>>.Success([TestReports.Create(), TestReports.Create()]));

        await _vm.OnNavigatedToAsync(null);

        Assert.That(_vm.Reports, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task OnNavigatedTo_NoReports_ShowsEmptyList()
    {
        _reports.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<IReadOnlyList<Report>>.Success([]));

        await _vm.OnNavigatedToAsync(null);

        Assert.That(_vm.Reports, Is.Empty);
    }

    [Test]
    public async Task OnNavigatedTo_ServiceFails_ShowsErrorOnce()
    {
        _reports.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))         // Arrange: usługa zwraca błąd
                .ReturnsAsync(Result<IReadOnlyList<Report>>.Failure(Errors.Network));

        await _vm.OnNavigatedToAsync(null);                                        // Act: wejście na stronę

        _feedback.Verify(f => f.ShowErrorAsync(Errors.Network), Times.Once);      // Assert: komunikat raz
        Assert.That(_vm.Reports, Is.Empty);                                        // … i pusta lista
    }

    // ----- nawigacja -----

    [Test]
    public async Task OpenReport_NavigatesToDetails_WithReportId()
    {
        var reportId = Guid.NewGuid();
        var item = new ReportItem(reportId, "Awaria pompy", "data", ReportPriority.High, "Wysoki",
            ReportStatus.New, "Nowe", true, null, true);

        await _vm.OpenReportCommand.ExecuteAsync(item);

        _navigation.Verify(n => n.NavigateToAsync<ReportDetailsViewModel>(reportId), Times.Once);
    }

    [Test]
    public async Task OnAppearing_AfterFirstLoad_RefreshesList()
    {
        _reports.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<IReadOnlyList<Report>>.Success([TestReports.Create()]));

        await _vm.OnAppearingAsync();          // pierwsze pojawienie się: bez ładowania
        await _vm.OnNavigatedToAsync(null);    // jednorazowe ładowanie
        await _vm.OnAppearingAsync();          // powrót ze szczegółów: odświeżenie

        _reports.Verify(r => r.GetAllAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    // ----- Odświeżenie po komunikacie (powiadomienie push na pierwszym planie, synchronizacja) -----

    [Test]
    public async Task ReportsChangedMessage_AfterLoad_ReloadsList()
    {
        _reports.SetupSequence(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<IReadOnlyList<Report>>.Success([]))
                .ReturnsAsync(Result<IReadOnlyList<Report>>.Success([TestReports.Create()]));
        await _vm.OnNavigatedToAsync(null);

        _messenger.Send(new ReportsChangedMessage());

        Assert.That(() => _vm.Reports, Has.Count.EqualTo(1).After(1000, 20));
    }

    [Test]
    public void ReportsChangedMessage_BeforeNavigation_IsIgnored()
    {
        _messenger.Send(new ReportsChangedMessage()); // ViewModel jeszcze niezarejestrowany
        _reports.Verify(r => r.GetAllAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // ----- usuwanie -----

    private async Task<ReportItem> LoadOnePendingAsync()
    {
        _reports.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<IReadOnlyList<Report>>.Success([TestReports.Create(syncState: SyncState.Pending)]));
        await _vm.OnNavigatedToAsync(null);
        return _vm.Reports.Single();
    }

    [Test]
    public async Task DeleteReport_NotConfirmed_DoesNotCallService()
    {
        var item = await LoadOnePendingAsync();
        _feedback.Setup(f => f.ConfirmAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(false);

        await _vm.DeleteReportCommand.ExecuteAsync(item);

        _reports.Verify(r => r.DeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        Assert.That(_vm.Reports, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task DeleteReport_ServiceFails_ShowsError_AndItemStays()
    {
        var item = await LoadOnePendingAsync();
        _feedback.Setup(f => f.ConfirmAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(true);
        _reports.Setup(r => r.DeleteAsync(item.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Failure(Errors.ReportAlreadySynced));

        await _vm.DeleteReportCommand.ExecuteAsync(item);

        _feedback.Verify(f => f.ShowErrorAsync(Errors.ReportAlreadySynced), Times.Once);
        Assert.That(_vm.Reports, Does.Contain(item));
    }

    [Test]
    public async Task DeleteReport_Confirmed_RemovesItem()
    {
        var item = await LoadOnePendingAsync();
        _feedback.Setup(f => f.ConfirmAsync("Reports_DeleteTitle", "Reports_DeleteMessage")).ReturnsAsync(true);
        _reports.Setup(r => r.DeleteAsync(item.Id, It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success());

        await _vm.DeleteReportCommand.ExecuteAsync(item);

        Assert.That(_vm.Reports, Is.Empty);
        Assert.That(_vm.Reports, Is.Empty);
    }

    [Test]
    public async Task DeleteReport_CanExecute_OnlyForNotSentReports()
    {
        var pending = await LoadOnePendingAsync();
        var synced = pending with { IsPendingSync = false, CanDelete = false };
        var sentWithLocalChanges = pending with { IsPendingSync = true, CanDelete = false };

        Assert.That(_vm.DeleteReportCommand.CanExecute(pending), Is.True);
        Assert.That(_vm.DeleteReportCommand.CanExecute(synced), Is.False);
        Assert.That(_vm.DeleteReportCommand.CanExecute(sentWithLocalChanges), Is.False);
    }
}
