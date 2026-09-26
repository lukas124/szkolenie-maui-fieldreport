using FieldReport.Core.Models;
using FieldReport.Core.ValueObjects;

namespace FieldReport.Core.Data.Api;

/// <summary>Report ↔ ReportDto (jedna odpowiedzialność). Zgłoszenie z serwera jest wysłane (Synced, IsOnServer).</summary>
public static class ReportDtoMapper
{
    public static ReportDto ToDto(Report report) => new(
        report.Id, report.Title.Value, report.Description, report.Priority, report.Status, report.CreatedAt, report.UpdatedAt);

    public static Report ToModel(ReportDto dto) => new(
        dto.Id,
        ReportTitle.Create(dto.Title).Value ?? throw new InvalidDataException($"Invalid title in report {dto.Id} from API."),
        dto.Description,
        dto.Priority,
        dto.Status,
        SyncState.Synced,
        photoPath: null,
        dto.CreatedAt,
        dto.UpdatedAt,
        isOnServer: true,
        photoUrl: dto.PhotoUrl); // podpisany adres zdjęcia na serwerze (Image pobierze je sam)
}
