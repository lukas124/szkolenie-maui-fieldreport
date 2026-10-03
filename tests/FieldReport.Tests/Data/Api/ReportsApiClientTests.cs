using System.Net;
using FieldReport.Core.Data.Api;
using FieldReport.Core.Models;
using FieldReport.Tests.TestData;
using Polly.CircuitBreaker;
using Polly.Timeout;

namespace FieldReport.Tests.Data.Api;

[TestFixture]
public class ReportsApiClientTests
{
    private const string ReportJson = """
        [{"id":"3f2c9a4e-8d1b-4c7a-9e55-0a1b2c3d4e5f","title":"Awaria pompy","description":null,"priority":"High","status":"InProgress",
          "createdAt":"2026-09-26T08:15:00+00:00","updatedAt":"2026-09-26T08:20:31.1234567+00:00","hasPhoto":true,
          "photoUrl":"https://api.test/maui/podstawowe/api/reports/3f2c9a4e-8d1b-4c7a-9e55-0a1b2c3d4e5f/photo"}]
        """;

    [Test]
    public async Task GetReports_ParsesContract_WithEnumsAsText()
    {
        var handler = new StubHttpHandler().Respond(HttpStatusCode.OK, ReportJson);

        var result = await new ReportsApiClient(handler.CreateClient()).GetReportsAsync(default);

        var dto = result.Value!.Single();
        Assert.Multiple(() =>
        {
            Assert.That(dto.Id, Is.EqualTo(Guid.Parse("3f2c9a4e-8d1b-4c7a-9e55-0a1b2c3d4e5f")));
            Assert.That(dto.Priority, Is.EqualTo(ReportPriority.High));
            Assert.That(dto.Status, Is.EqualTo(ReportStatus.InProgress));
            Assert.That(dto.HasPhoto, Is.True);
            Assert.That(dto.PhotoUrl, Does.StartWith("https://api.test/"));
            Assert.That(dto.UpdatedAt, Is.EqualTo(DateTimeOffset.Parse("2026-09-26T08:20:31.1234567+00:00")));
        });
        Assert.That(handler.Requests.Single().Request.RequestUri!.PathAndQuery, Is.EqualTo("/maui/podstawowe/api/reports"));
    }

    [Test]
    [Ignore("Ćwiczenie 12: usuń Ignore po uzupełnieniu TODO")]
    public async Task GetReport_UsesIdInPath()
    {
        var handler = new StubHttpHandler().Respond(HttpStatusCode.OK, ReportJson.Trim().TrimStart('[').TrimEnd(']'));
        var id = Guid.Parse("3f2c9a4e-8d1b-4c7a-9e55-0a1b2c3d4e5f");

        var result = await new ReportsApiClient(handler.CreateClient()).GetReportAsync(id, default);

        Assert.That(result.Value!.Id, Is.EqualTo(id));
        Assert.That(handler.Requests.Single().Request.RequestUri!.AbsolutePath, Is.EqualTo($"/maui/podstawowe/api/reports/{id}"));
    }

    [Test]
    public async Task Create_IsPostWithIdempotencyKeyHeader_AndReturnsReportWithIdFromServer()
    {
        var handler = new StubHttpHandler().Respond(HttpStatusCode.Created, ReportJson.Trim().TrimStart('[').TrimEnd(']'));
        var key = Guid.Parse("8e03978e-40d5-43e8-bc93-6894a57f9324");
        var dto = new ReportDto(Guid.NewGuid(), "Awaria", "Opis", ReportPriority.Low, ReportStatus.New,
            new DateTimeOffset(2026, 9, 26, 8, 15, 0, TimeSpan.Zero), new DateTimeOffset(2026, 9, 26, 8, 15, 0, TimeSpan.Zero));

        var result = await new ReportsApiClient(handler.CreateClient()).CreateAsync(dto, key, default);

        Assert.That(result.Value!.Id, Is.EqualTo(Guid.Parse("3f2c9a4e-8d1b-4c7a-9e55-0a1b2c3d4e5f"))); // Id z serwera
        var (request, body) = handler.Requests.Single();
        Assert.That(request.Method, Is.EqualTo(HttpMethod.Post));
        Assert.That(request.RequestUri!.AbsolutePath, Is.EqualTo("/maui/podstawowe/api/reports"));
        Assert.That(request.Headers.GetValues("Idempotency-Key").Single(), Is.EqualTo("\"8e03978e-40d5-43e8-bc93-6894a57f9324\""));
        Assert.That(body, Does.Contain("\"priority\":\"Low\"").And.Contain("\"title\":\"Awaria\""));
        Assert.That(body, Does.Contain($"\"id\":\"{Guid.Empty}\"")); // Id nadaje serwer
    }

    [Test]
    public async Task Update_IsPutWithIdInPath_CamelCaseAndEnumText()
    {
        var handler = new StubHttpHandler().Respond(HttpStatusCode.OK, "{}");
        var dto = new ReportDto(Guid.Parse("3f2c9a4e-8d1b-4c7a-9e55-0a1b2c3d4e5f"), "Awaria", "Opis", ReportPriority.Low, ReportStatus.Done,
            new DateTimeOffset(2026, 9, 26, 8, 15, 0, TimeSpan.Zero), new DateTimeOffset(2026, 9, 26, 8, 15, 0, TimeSpan.Zero));

        var result = await new ReportsApiClient(handler.CreateClient()).UpdateAsync(dto, default);

        Assert.That(result.IsSuccess, Is.True);
        var (request, body) = handler.Requests.Single();
        Assert.That(request.Method, Is.EqualTo(HttpMethod.Put));
        Assert.That(request.RequestUri!.AbsolutePath, Is.EqualTo("/maui/podstawowe/api/reports/3f2c9a4e-8d1b-4c7a-9e55-0a1b2c3d4e5f"));
        Assert.That(body, Does.Contain("\"priority\":\"Low\"").And.Contain("\"status\":\"Done\"").And.Contain("\"title\":\"Awaria\""));
    }

    [TestCase(HttpStatusCode.UnprocessableEntity)] // ten sam klucz z inną treścią
    [TestCase(HttpStatusCode.Conflict)]            // ten sam klucz przetwarzany równolegle
    public async Task Create_RejectedKey_IsErrorInResult(HttpStatusCode status)
    {
        var handler = new StubHttpHandler().Respond(status, """{"title":"…","status":0}""");
        var dto = new ReportDto(Guid.NewGuid(), "Awaria", null, ReportPriority.Low, ReportStatus.New, default, default);

        var result = await new ReportsApiClient(handler.CreateClient()).CreateAsync(dto, Guid.NewGuid(), default);

        Assert.That(result.Error, Is.EqualTo(HttpErrorMapper.Map(status)));
    }

    [Test]
    public async Task UploadPhoto_IsMultipartWithPhotoField()
    {
        var file = Path.GetTempFileName();
        await File.WriteAllBytesAsync(file, [0xFF, 0xD8, 0xFF, 0xE0, 1, 2, 3]);
        var handler = new StubHttpHandler().Respond(HttpStatusCode.OK, "{}");

        await new ReportsApiClient(handler.CreateClient()).UploadPhotoAsync(Guid.Empty, file, default);

        var (request, body) = handler.Requests.Single();
        Assert.That(request.Content!.Headers.ContentType!.MediaType, Is.EqualTo("multipart/form-data"));
        Assert.That(body, Does.Contain("name=photo"));
        File.Delete(file);
    }

    [Test]
    public async Task PollyTimeout_IsNetworkError() =>
        Assert.That((await new ReportsApiClient(new StubHttpHandler().Throw(new TimeoutRejectedException()).CreateClient())
            .GetReportsAsync(default)).Error, Is.EqualTo(Errors.Network));

    [Test]
    public async Task OpenCircuit_IsNetworkError() =>
        Assert.That((await new ReportsApiClient(new StubHttpHandler().Throw(new BrokenCircuitException()).CreateClient())
            .UpdateAsync(new ReportDto(Guid.NewGuid(), "Awaria", null, ReportPriority.Low, ReportStatus.New, default, default), default)).Error,
            Is.EqualTo(Errors.Network));
}

[TestFixture]
public class ReportDtoMapperTests
{
    [Test]
    public void RoundTrip_KeepsFields()
    {
        var report = TestReports.Create(title: "Awaria", status: ReportStatus.InProgress, priority: ReportPriority.High);
        var model = ReportDtoMapper.ToModel(ReportDtoMapper.ToDto(report));
        Assert.That((model.Id, model.Title.Value, model.Status, model.Priority, model.CreatedAt),
            Is.EqualTo((report.Id, "Awaria", ReportStatus.InProgress, ReportPriority.High, report.CreatedAt)));
        Assert.That(model.SyncState, Is.EqualTo(SyncState.Synced));
        Assert.That(model.IsOnServer, Is.True);
    }

    [Test]
    public void PhotoUrlFromServer_IsKeptOnModel() // podpisany adres zdjęcia — Image pobiera je bez tokenu
    {
        var dto = new ReportDto(Guid.NewGuid(), "Awaria", null, ReportPriority.Low, ReportStatus.New, default, default,
            HasPhoto: true, PhotoUrl: "https://api.test/photo?exp=1&sig=abc");

        Assert.That(ReportDtoMapper.ToModel(dto).PhotoUrl, Is.EqualTo("https://api.test/photo?exp=1&sig=abc"));
    }

    [Test]
    public void InvalidTitleFromServer_Throws() =>
        Assert.That(() => ReportDtoMapper.ToModel(new ReportDto(Guid.NewGuid(), "x", null, ReportPriority.Low, ReportStatus.New, default, default)),
            Throws.InstanceOf<InvalidDataException>());
}
