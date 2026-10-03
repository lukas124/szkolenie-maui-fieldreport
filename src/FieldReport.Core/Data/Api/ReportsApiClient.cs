using System.Net.Http.Headers;
using System.Net.Http.Json;
using FieldReport.Core.Models;

namespace FieldReport.Core.Data.Api;

/// <summary>
/// Klient HTTP zgłoszeń (typed client), napisany ręcznie, żeby było widać każde żądanie. W większych projektach
/// taki kod generuje się z opisu OpenAPI (np. NSwag). Ścieżki są względne — BaseAddress kończy się „/”.
/// </summary>
public sealed class ReportsApiClient(HttpClient http) : IReportsApiClient
{
    public Task<Result<IReadOnlyList<ReportDto>>> GetReportsAsync(CancellationToken ct = default) =>
        http.SendForResultAsync<IReadOnlyList<ReportDto>>(new HttpRequestMessage(HttpMethod.Get, "api/reports"), ct);

    // TODO: GET $"api/reports/{id}" przez http.SendForResultAsync<ReportDto>(…) (wzór: GetReportsAsync wyżej)
    public Task<Result<ReportDto>> GetReportAsync(Guid id, CancellationToken ct = default) =>
        Task.FromResult(Result<ReportDto>.Failure(Errors.Network));

    // Nowe zgłoszenie: Id nada serwer (wysyłamy puste), klucz idempotencji w nagłówku —
    // wg standardu IETF wartość to napis w cudzysłowie, np. Idempotency-Key: "8e03978e-…"
    public Task<Result<ReportDto>> CreateAsync(ReportDto report, Guid idempotencyKey, CancellationToken ct = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "api/reports")
        {
            Content = JsonContent.Create(report with { Id = Guid.Empty }, options: ApiJson.Options)
        };
        request.Headers.Add("Idempotency-Key", $"\"{idempotencyKey}\"");
        return http.SendForResultAsync<ReportDto>(request, ct);
    }

    public Task<Result> UpdateAsync(ReportDto report, CancellationToken ct = default) =>
        http.SendForResultAsync(new HttpRequestMessage(HttpMethod.Put, $"api/reports/{report.Id}")
        {
            Content = JsonContent.Create(report, options: ApiJson.Options)
        }, ct);

    // Bajty zdjęcia w pamięci (ByteArrayContent), żeby żądanie dało się wysłać ponownie po odświeżeniu tokenu
    public async Task<Result> UploadPhotoAsync(Guid reportId, string photoPath, CancellationToken ct = default)
    {
        var bytes = await File.ReadAllBytesAsync(photoPath, ct);
        var photo = new ByteArrayContent(bytes);
        photo.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        var form = new MultipartFormDataContent { { photo, "photo", Path.GetFileName(photoPath) } };
        return await http.SendForResultAsync(new HttpRequestMessage(HttpMethod.Put, $"api/reports/{reportId}/photo") { Content = form }, ct)
            ;
    }
}
