using FieldReport.Core.Models;
using FieldReport.Core.Navigation;
using FieldReport.Core.Presentation;
using FieldReport.Core.Services;
using FieldReport.Core.ViewModels;
using FieldReport.Tests.TestData;

namespace FieldReport.Tests.ViewModels;

[TestFixture]
public class ReportsViewModelTests
{
    private Mock<INavigationService> _navigation = null!;
    private Mock<IReportService> _reports = null!;
    private Mock<IUserFeedback> _feedback = null!;
    private ReportsViewModel _vm = null!;

    [SetUp]
    public void SetUp()
    {
        _navigation = new Mock<INavigationService>();
        _reports = new Mock<IReportService>();
        _feedback = new Mock<IUserFeedback>();
        var items = new Mock<IReportItemFactory>();
        items.Setup(f => f.Create(It.IsAny<Report>())).Returns((Report r) => new ReportItem(
            r.Id, r.Title.Value, "data", r.Priority, "priorytet", r.Status, "status", r.SyncState == SyncState.Pending, null));

        _vm = new ReportsViewModel(_navigation.Object, _reports.Object, items.Object, _feedback.Object);
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
}
