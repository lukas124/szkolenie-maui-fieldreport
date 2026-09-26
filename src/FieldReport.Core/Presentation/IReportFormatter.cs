using FieldReport.Core.Models;

namespace FieldReport.Core.Presentation;

/// <summary>Teksty dla użytkownika (daty, statusy, priorytety) — testowalne, kultura pod kontrolą.</summary>
public interface IReportFormatter
{
    string FormatDate(DateTimeOffset value);
    string FormatStatus(ReportStatus status);
    string FormatPriority(ReportPriority priority);
    string FormatSyncState(SyncState syncState);
}
