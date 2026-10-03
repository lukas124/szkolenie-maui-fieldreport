using FieldReport.Core.Models;
using FieldReport.Core.Navigation;
using FieldReport.Core.Presentation;
using FieldReport.Core.Services;
using FieldReport.Core.ViewModels;
using FieldReport.Tests.TestData;

namespace FieldReport.Tests.ViewModels;

[TestFixture]
public class ReportDetailsViewModelTests
{
    private Mock<IReportService> _reports = null!;
    private Mock<IUserFeedback> _feedback = null!;
    private Mock<INavigationService> _navigation = null!;
    private ReportDetailsViewModel _vm = null!;
    private Report _report = null!;

    [SetUp]
    public void SetUp()
    {
        _reports = new Mock<IReportService>();
        _feedback = new Mock<IUserFeedback>();
        var details = new Mock<IReportDetailsFactory>();
        details.Setup(d => d.Create(It.IsAny<Report>())).Returns((Report r) => new ReportDetails(
            r.Id, r.Title.Value, "opis", r.Priority, "p", r.Status, r.Status.ToString(), "c", "u", false, "s", null,
            r.AllowedNextStatuses.Select(s => new StatusAction(s, s.ToString())).ToList()));

        _report = TestReports.Create(status: ReportStatus.New);
        _reports.Setup(r => r.GetAsync(_report.Id, It.IsAny<CancellationToken>())).ReturnsAsync(Result<Report>.Success(_report));

        _navigation = new Mock<INavigationService>();
        _vm = new ReportDetailsViewModel(_navigation.Object, _reports.Object, details.Object, _feedback.Object);
    }

    [Test]
    public async Task OnNavigatedTo_LoadsReport_WithActions()
    {
        await _vm.OnNavigatedToAsync(_report.Id);

        Assert.That(_vm.Report!.Id, Is.EqualTo(_report.Id));
        Assert.That(_vm.Report.Actions.Select(a => a.TargetStatus), Is.EqualTo(new[] { ReportStatus.InProgress }));
    }

    [Test]
    public async Task ChangeStatus_CallsService_AndShowsUpdatedReport()
    {
        await _vm.OnNavigatedToAsync(_report.Id);
        var updated = TestReports.Create(status: ReportStatus.InProgress, id: _report.Id);
        _reports.Setup(r => r.ChangeStatusAsync(_report, ReportStatus.InProgress, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Report>.Success(updated));

        await _vm.ChangeStatusCommand.ExecuteAsync(_vm.Report!.Actions[0]);

        Assert.That(_vm.Report!.Status, Is.EqualTo(ReportStatus.InProgress));
    }

    [Test]
    public async Task ChangeStatus_ServiceFails_ShowsError()
    {
        await _vm.OnNavigatedToAsync(_report.Id);
        _reports.Setup(r => r.ChangeStatusAsync(_report, It.IsAny<ReportStatus>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Report>.Failure(Errors.InvalidStatusTransition));

        await _vm.ChangeStatusCommand.ExecuteAsync(_vm.Report!.Actions[0]);

        _feedback.Verify(f => f.ShowErrorAsync(Errors.InvalidStatusTransition), Times.Once);
        Assert.That(_vm.Report!.Status, Is.EqualTo(ReportStatus.New));
    }

    [Test]
    public async Task OnNavigatedTo_ReportMissing_ShowsInfo_AndGoesBackToList() // np. lokalne Id zgłoszenia wysłanego w tle
    {
        var missing = Guid.NewGuid();
        _reports.Setup(r => r.GetAsync(missing, It.IsAny<CancellationToken>())).ReturnsAsync(Result<Report>.Failure(Errors.ReportNotFound));

        await _vm.OnNavigatedToAsync(missing);

        _feedback.Verify(f => f.ShowInfoAsync("Details_NotFound_Title", "Details_NotFound_Message"), Times.Once);
        _navigation.Verify(n => n.GoBackAsync(), Times.Once);
        Assert.That(_vm.Report, Is.Null);
    }

    [Test]
    public async Task OnNavigatedTo_OtherError_ShowsError_AndStays()
    {
        var id = Guid.NewGuid();
        _reports.Setup(r => r.GetAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(Result<Report>.Failure(Errors.Network));

        await _vm.OnNavigatedToAsync(id);

        _feedback.Verify(f => f.ShowErrorAsync(Errors.Network), Times.Once);
        _navigation.Verify(n => n.GoBackAsync(), Times.Never);
    }
}
