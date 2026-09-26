namespace FieldReport.Core.Services;

/// <summary>Zapis/skalowanie zdjęcia w katalogu aplikacji (zdjęcia z aparatu są duże: pamięć, transfer).</summary>
public interface IPhotoStorage
{
    Task<MediaFile> StoreAsync(Stream source, CancellationToken ct = default);
}
