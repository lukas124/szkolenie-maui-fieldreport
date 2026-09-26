using FieldReport.Core.Models;
using FieldReport.Core.ValueObjects;

namespace FieldReport.Core.Data;

/// <summary>Jedna odpowiedzialność: ReportRow ↔ Report (tabela nie zna reguł modelu, model nie zna tabeli).</summary>
public static class ReportRowMapper
{
    // Zgłoszenie z lokalnej bazy zawsze czeka na wysłanie (Pending)
    public static Report ToModel(ReportRow row) => new(
        row.Id,
        ReportTitle.Create(row.Title).Value ?? throw new InvalidDataException($"Invalid title in report {row.Id}."),
        row.Description,
        (ReportPriority)row.Priority,
        (ReportStatus)row.Status,
        SyncState.Pending,
        row.PhotoPath,
        row.CreatedAt,
        row.UpdatedAt,
        isOnServer: false);

    public static ReportRow ToRow(Report report) => new()
    {
        Id = report.Id,
        Title = report.Title.Value,
        Description = report.Description,
        Priority = (int)report.Priority,
        Status = (int)report.Status,
        PhotoPath = report.PhotoPath,
        CreatedAt = report.CreatedAt,
        UpdatedAt = report.UpdatedAt
    };
}
