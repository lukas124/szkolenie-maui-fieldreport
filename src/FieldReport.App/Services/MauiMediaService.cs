using FieldReport.Core.Models;
using FieldReport.Core.Services;
using Microsoft.Extensions.Logging;

namespace FieldReport.App.Services;

/// <summary>
/// Aparat i galeria przez MediaPicker. Jedna odpowiedzialność: pozyskać plik; uprawnienia → IPermissionService,
/// zapis i skalowanie → IPhotoStorage. Wyjątki platformy zamieniamy na Result (anulowanie ≠ błąd).
/// </summary>
public sealed class MauiMediaService(IPermissionService permissions, IPhotoStorage storage, ILogger<MauiMediaService> logger)
    : IMediaService
{
    // .NET 10: RotateImage obraca zdjęcie wg EXIF (inaczej zdjęcie z aparatu bywa „na boku”)
    private static readonly MediaPickerOptions Options = new() { RotateImage = true, SelectionLimit = 1 };

    public async Task<Result<MediaFile>> CapturePhotoAsync(CancellationToken ct = default)
    {
        var access = await permissions.EnsureCameraAsync();
        if (!access.IsSuccess) return Result<MediaFile>.Failure(access.Error!);
        if (!MediaPicker.Default.IsCaptureSupported) return Result<MediaFile>.Failure(Errors.Media.NotSupported);

        return await StoreAsync(() => MediaPicker.Default.CapturePhotoAsync(Options), ct);
    }

    // Galeria: Android 13+ i iOS 14+ używają systemowego selektora zdjęć — bez uprawnienia do całej galerii
    public Task<Result<MediaFile>> PickPhotoAsync(CancellationToken ct = default) =>
        StoreAsync(async () => (await MediaPicker.Default.PickPhotosAsync(Options)).FirstOrDefault(), ct);

    private async Task<Result<MediaFile>> StoreAsync(Func<Task<FileResult?>> pick, CancellationToken ct) // aparat i galeria
    {
        try
        {
            var photo = await MainThread.InvokeOnMainThreadAsync(pick);
            if (photo is null) return Result<MediaFile>.Failure(Errors.Media.Cancelled);

            MediaFile stored;
            await using (var stream = await photo.OpenReadAsync())
                stored = await storage.StoreAsync(stream, ct); // przeskalowana kopia w katalogu aplikacji

            DeleteTemporaryCopy(photo);    // pełnowymiarowa kopia z cache – już zbędna
            return Result<MediaFile>.Success(stored);
        }
        catch (OperationCanceledException)
        {
            return Result<MediaFile>.Failure(Errors.Media.Cancelled);
        }
        // brak aparatu (np. symulator iOS)
        catch (FeatureNotSupportedException)
        {
            return Result<MediaFile>.Failure(Errors.Media.NotSupported);
        }
        // brak uprawnienia
        catch (PermissionException)
        {
            return Result<MediaFile>.Failure(Errors.PermissionDenied);
        }
        // zapis pliku, zły obraz
        catch (Exception ex) when (ex is IOException or InvalidOperationException or ArgumentException)
        {
            logger.LogError(ex, "Photo could not be stored");
            return Result<MediaFile>.Failure(Errors.Media.Failed);
        }
    }

    // MediaPicker zapisuje zrobione/wybrane zdjęcie (pełny rozmiar, kilka–kilkadziesiąt MB) w katalogu cache aplikacji.
    // Po zapisaniu przeskalowanej wersji ta kopia nie jest potrzebna. Usuwamy wyłącznie pliki z cache aplikacji —
    // nigdy oryginału z galerii telefonu.
    private void DeleteTemporaryCopy(FileResult photo)
    {
        try
        {
            if (InAppCache(photo.FullPath) && File.Exists(photo.FullPath))
                File.Delete(photo.FullPath);
        }
        catch (IOException ex)
        {
            // nie przerywamy: zdjęcie jest już zapisane
            logger.LogWarning(ex, "Temporary photo copy not deleted");
        }
    }

    // Czy plik leży w katalogu cache aplikacji. Android pokazuje katalog aplikacji pod dwiema ścieżkami
    // (/data/data/<pakiet> i /data/user/0/<pakiet>) — przed porównaniem sprowadzamy obie do jednej postaci.
    private static bool InAppCache(string path)
    {
        static string Normalize(string p) => p.Replace("/data/data/", "/data/user/0/");
        return Normalize(path).StartsWith(Normalize(FileSystem.CacheDirectory), StringComparison.Ordinal);
    }
}
