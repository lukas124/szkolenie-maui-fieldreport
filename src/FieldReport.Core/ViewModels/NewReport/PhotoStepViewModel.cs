using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FieldReport.Core.Models;
using FieldReport.Core.Presentation;
using FieldReport.Core.Services;

namespace FieldReport.Core.ViewModels.NewReport;

/// <summary>
/// Krok „Zdjęcie” (opcjonalne): zrób / wybierz / usuń + podgląd. Krok orkiestruje: usługa → wynik → stan widoku.
/// Anulowanie wyboru nie jest błędem; odmowa na stałe → wyjaśnienie + przejście do ustawień.
/// </summary>
public sealed partial class PhotoStepViewModel : WizardStepViewModel
{
    private readonly IMediaService _media;
    private readonly IPermissionService _permissions;
    private readonly IUserFeedback _feedback;
    private readonly ILocalizer _localizer;
    private NewReportDraft _draft = null!;

    public PhotoStepViewModel(IMediaService media, IPermissionService permissions, IUserFeedback feedback, ILocalizer localizer)
    {
        _media = media;
        _permissions = permissions;
        _feedback = feedback;
        _localizer = localizer;
        IsValid = true; // zdjęcie opcjonalne: „Dalej” zawsze aktywne
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasPhoto))]
    [NotifyCanExecuteChangedFor(nameof(RemovePhotoCommand))]
    private string? _photoPath;      // ścieżka do podglądu

    public bool HasPhoto => PhotoPath is not null;

    public override string Title => _localizer.GetString("NewReport_PhotoTitle");

    public override void Attach(NewReportDraft draft) => _draft = draft;

    public override void OnShown() => PhotoPath = _draft.PhotoPath;

    [RelayCommand]
    private async Task CapturePhotoAsync(CancellationToken ct)
    {
        var result = await _media.CapturePhotoAsync(ct);
        if (result.IsSuccess) { SetPhoto(result.Value!); return; }
        await HandleErrorAsync(result.Error!);
    }

    [RelayCommand]
    private async Task PickPhotoAsync(CancellationToken ct)
    {
        var result = await _media.PickPhotoAsync(ct);
        if (result.IsSuccess) { SetPhoto(result.Value!); return; }
        await HandleErrorAsync(result.Error!);
    }

    [RelayCommand(CanExecute = nameof(HasPhoto))]
    private void RemovePhoto()
    {
        _draft.PhotoPath = null;
        PhotoPath = null;
    }

    private void SetPhoto(MediaFile file)
    {
        _draft.PhotoPath = file.Path;
        PhotoPath = file.Path;
    }

    private async Task HandleErrorAsync(Error error)
    {
        if (error == Errors.Media.Cancelled) return;   // 1. anulowanie to nie błąd

        if (error == Errors.PermissionDeniedPermanently) // 2. tylko Ustawienia pomogą
        {
            if (await _feedback.ConfirmAsync("Permission_CameraTitle", "Permission_CameraSettingsMessage",
                    "Permission_OpenSettings", "Common_Cancel"))
                _permissions.OpenAppSettings();
            return;
        }

        await _feedback.ShowErrorAsync(error);         // 3. zwykły komunikat
    }
}
