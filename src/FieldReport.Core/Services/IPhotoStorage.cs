namespace FieldReport.Core.Services;

/// <summary>Zapis/skalowanie zdjęcia w katalogu aplikacji (zdjęcia z aparatu są duże: pamięć, transfer).</summary>
public interface IPhotoStorage
{
    Task<MediaFile> StoreAsync(Stream source, CancellationToken ct = default);

    /// <summary>Usuwa jeden plik zdjęcia (np. po wysłaniu go do API).</summary>
    Task DeleteAsync(string path, CancellationToken ct = default);

    /// <summary>Usuwa wszystkie zapisane zdjęcia (wylogowanie: dane technika nie zostają w telefonie).</summary>
    Task DeleteAllAsync(CancellationToken ct = default);
}
