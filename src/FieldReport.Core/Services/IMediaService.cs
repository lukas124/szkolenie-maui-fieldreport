using FieldReport.Core.Models;

namespace FieldReport.Core.Services;

/// <summary>
/// Pozyskanie zdjęcia (aparat/galeria + uprawnienia). Implementacja w App (MediaPicker, Permissions).
/// Anulowanie = <see cref="Errors.Media.Cancelled"/>, odmowa = <see cref="Errors.PermissionDenied"/> /
/// <see cref="Errors.PermissionDeniedPermanently"/>.
/// </summary>
public interface IMediaService
{
    Task<Result<MediaFile>> CapturePhotoAsync(CancellationToken ct = default);
    Task<Result<MediaFile>> PickPhotoAsync(CancellationToken ct = default);
}
