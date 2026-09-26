using FieldReport.Core.Services;
using Microsoft.Maui.Graphics.Platform;
using IImage = Microsoft.Maui.Graphics.IImage;

namespace FieldReport.App.Services;

/// <summary>
/// Zapis zdjęcia w katalogu aplikacji ze skalowaniem (dłuższy bok maks. 1600 px, JPEG 85%): mniej pamięci
/// przy wyświetlaniu i mniejszy transfer przy wysyłce. Oryginał z aparatu (kilka MB) nie trafia do bazy ani API.
/// </summary>
public sealed class PhotoStorage(IAppPaths paths) : IPhotoStorage
{
    public const float MaxSide = 1600;          // dłuższy bok po zmniejszeniu
    private const float JpegQuality = 0.85f;

    private string PhotosDirectory => Path.Combine(paths.DataDirectory, "photos");

    public async Task<MediaFile> StoreAsync(Stream source, CancellationToken ct = default)
    {
        Directory.CreateDirectory(PhotosDirectory);
        var path = Path.Combine(PhotosDirectory, $"{Guid.NewGuid():N}.jpg");

        IImage image = PlatformImage.FromStream(source);
        if (Math.Max(image.Width, image.Height) > MaxSide)
            image = image.Downsize(MaxSide, disposeOriginal: true);

        await using (var output = File.Create(path))
            await image.SaveAsync(output, ImageFormat.Jpeg, JpegQuality);
        image.Dispose();

        return new MediaFile(path);
    }
}
