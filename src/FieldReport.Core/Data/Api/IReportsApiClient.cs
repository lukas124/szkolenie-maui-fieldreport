using FieldReport.Core.Models;

namespace FieldReport.Core.Data.Api;

/// <summary>Zgłoszenia w API szkoleniowym (wymaga zalogowania — token dokleja AuthHeaderHandler).</summary>
public interface IReportsApiClient
{
    /// <summary>GET api/reports — wszystkie zgłoszenia technika.</summary>
    Task<Result<IReadOnlyList<ReportDto>>> GetReportsAsync(CancellationToken ct = default);

    /// <summary>GET api/reports/{id} — jedno zgłoszenie (np. po tapnięciu powiadomienia).</summary>
    Task<Result<ReportDto>> GetReportAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// POST api/reports z nagłówkiem Idempotency-Key — nowe zgłoszenie; zwraca je z Id nadanym przez serwer.
    /// Ponowienie z tym samym kluczem nie tworzy duplikatu (serwer zwraca to samo zgłoszenie).
    /// </summary>
    Task<Result<ReportDto>> CreateAsync(ReportDto report, Guid idempotencyKey, CancellationToken ct = default);

    /// <summary>PUT api/reports/{id} — aktualizacja zgłoszenia, które już jest na serwerze (np. zmiana statusu).</summary>
    Task<Result> UpdateAsync(ReportDto report, CancellationToken ct = default);

    /// <summary>PUT api/reports/{id}/photo — wysyła plik zdjęcia (multipart/form-data, pole „photo”).</summary>
    Task<Result> UploadPhotoAsync(Guid reportId, string photoPath, CancellationToken ct = default);
}
