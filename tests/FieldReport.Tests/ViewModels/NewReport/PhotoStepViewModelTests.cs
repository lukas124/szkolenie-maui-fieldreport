using FieldReport.Core.Models;
using FieldReport.Core.Presentation;
using FieldReport.Core.Services;
using FieldReport.Core.ViewModels.NewReport;

namespace FieldReport.Tests.ViewModels.NewReport;

[TestFixture]
[Ignore("Ćwiczenie 09: usuń Ignore po uzupełnieniu TODO")]
public class PhotoStepViewModelTests
{
    private Mock<IMediaService> _media = null!;
    private Mock<IPermissionService> _permissions = null!;
    private Mock<IUserFeedback> _feedback = null!;
    private NewReportDraft _draft = null!;
    private PhotoStepViewModel _step = null!;

    [SetUp]
    public void SetUp()
    {
        _media = new Mock<IMediaService>();
        _permissions = new Mock<IPermissionService>();
        _feedback = new Mock<IUserFeedback>();
        _step = new PhotoStepViewModel(_media.Object, _permissions.Object, _feedback.Object, Mock.Of<ILocalizer>());
        _draft = new NewReportDraft();
        _step.Attach(_draft);
    }

    [Test]
    public void IsOptional_NextAlwaysEnabled() => Assert.That(_step.IsValid, Is.True);

    [Test]
    public async Task Capture_Success_ShowsPreview_AndWritesDraft()
    {
        _media.Setup(m => m.CapturePhotoAsync(It.IsAny<CancellationToken>()))
              .ReturnsAsync(Result<MediaFile>.Success(new MediaFile("/data/photos/a.jpg")));

        await _step.CapturePhotoCommand.ExecuteAsync(null);

        Assert.That(_step.PhotoPath, Is.EqualTo("/data/photos/a.jpg"));
        Assert.That(_step.HasPhoto, Is.True);
        Assert.That(_draft.PhotoPath, Is.EqualTo("/data/photos/a.jpg"));
    }

    [Test]
    public async Task Capture_Cancelled_ShowsNoMessage()
    {
        _media.Setup(m => m.CapturePhotoAsync(It.IsAny<CancellationToken>()))
              .ReturnsAsync(Result<MediaFile>.Failure(Errors.Media.Cancelled));

        await _step.CapturePhotoCommand.ExecuteAsync(null);

        _feedback.Verify(f => f.ShowErrorAsync(It.IsAny<Error>()), Times.Never);
        _feedback.Verify(f => f.ConfirmAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        Assert.That(_step.HasPhoto, Is.False);
    }

    [Test]
    public async Task Capture_PermissionDenied_ShowsError()
    {
        _media.Setup(m => m.CapturePhotoAsync(It.IsAny<CancellationToken>()))
              .ReturnsAsync(Result<MediaFile>.Failure(Errors.PermissionDenied));

        await _step.CapturePhotoCommand.ExecuteAsync(null);

        _feedback.Verify(f => f.ShowErrorAsync(Errors.PermissionDenied), Times.Once);
    }

    [Test]
    public async Task Capture_DeniedPermanently_ExplainsAndOpensSettings_WhenAccepted()
    {
        _media.Setup(m => m.CapturePhotoAsync(It.IsAny<CancellationToken>()))
              .ReturnsAsync(Result<MediaFile>.Failure(Errors.PermissionDeniedPermanently));
        _feedback.Setup(f => f.ConfirmAsync("Permission_CameraTitle", "Permission_CameraSettingsMessage", "Permission_OpenSettings", "Common_Cancel"))
                 .ReturnsAsync(true);

        await _step.CapturePhotoCommand.ExecuteAsync(null);

        _permissions.Verify(p => p.OpenAppSettings(), Times.Once);
        _feedback.Verify(f => f.ShowErrorAsync(It.IsAny<Error>()), Times.Never);
    }

    [Test]
    public async Task Capture_DeniedPermanently_Cancel_DoesNotOpenSettings()
    {
        _media.Setup(m => m.CapturePhotoAsync(It.IsAny<CancellationToken>()))
              .ReturnsAsync(Result<MediaFile>.Failure(Errors.PermissionDeniedPermanently));

        await _step.CapturePhotoCommand.ExecuteAsync(null);

        _permissions.Verify(p => p.OpenAppSettings(), Times.Never);
    }

    [Test]
    public async Task Pick_Cancelled_KeepsPreviousPhoto()
    {
        _draft.PhotoPath = "/data/photos/old.jpg";
        _step.OnShown();
        _media.Setup(m => m.PickPhotoAsync(It.IsAny<CancellationToken>()))
              .ReturnsAsync(Result<MediaFile>.Failure(Errors.Media.Cancelled));

        await _step.PickPhotoCommand.ExecuteAsync(null);

        Assert.That(_step.PhotoPath, Is.EqualTo("/data/photos/old.jpg"));
    }

    [Test]
    public async Task Pick_NotSupported_ShowsError()
    {
        _media.Setup(m => m.PickPhotoAsync(It.IsAny<CancellationToken>()))
              .ReturnsAsync(Result<MediaFile>.Failure(Errors.Media.NotSupported));

        await _step.PickPhotoCommand.ExecuteAsync(null);

        _feedback.Verify(f => f.ShowErrorAsync(Errors.Media.NotSupported), Times.Once);
    }

    [Test]
    public void Remove_ClearsPreviewAndDraft_AndIsDisabledWithoutPhoto()
    {
        Assert.That(_step.RemovePhotoCommand.CanExecute(null), Is.False);
        _draft.PhotoPath = "/data/photos/a.jpg";
        _step.OnShown();
        Assert.That(_step.RemovePhotoCommand.CanExecute(null), Is.True);

        _step.RemovePhotoCommand.Execute(null);

        Assert.That(_step.HasPhoto, Is.False);
        Assert.That(_draft.PhotoPath, Is.Null);
    }
}
