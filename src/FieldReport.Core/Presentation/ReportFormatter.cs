using FieldReport.Core.Models;
using FieldReport.Core.Services;

namespace FieldReport.Core.Presentation;

public sealed class ReportFormatter(ILocalizer localizer) : IReportFormatter
{
    public string FormatDate(DateTimeOffset value) => value.ToLocalTime().ToString("g", localizer.CurrentCulture);
    public string FormatStatus(ReportStatus status) => localizer.GetString($"ReportStatus_{status}");
    public string FormatPriority(ReportPriority priority) => localizer.GetString($"ReportPriority_{priority}");
    public string FormatSyncState(SyncState syncState) => localizer.GetString($"SyncState_{syncState}");
}
