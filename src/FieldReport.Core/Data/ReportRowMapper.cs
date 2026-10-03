using FieldReport.Core.Models;

namespace FieldReport.Core.Data;

/// <summary>Jedna odpowiedzialność: ReportRow ↔ Report (tabela nie zna reguł modelu, model nie zna tabeli).</summary>
public static class ReportRowMapper
{
    // TODO: wiersz → model: ReportTitle.Create(row.Title).Value, int → enum (Priority, Status), SyncState.Pending, isOnServer: false
    public static Report ToModel(ReportRow row) => throw new NotImplementedException();

    // TODO: model → wiersz: report.Title.Value, enum → int, pozostałe pola bez zmian
    public static ReportRow ToRow(Report report) => throw new NotImplementedException();
}
