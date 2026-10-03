using FieldReport.Core.Models;
using FieldReport.Core.Services;

namespace FieldReport.Core.Presentation;

/// <summary>Report → ReportDetails. Nowy status w modelu = nowa akcja bez zmian w ViewModelu i XAML.</summary>
public sealed class ReportDetailsFactory(IReportFormatter formatter, ILocalizer localizer) : IReportDetailsFactory
{
    public ReportDetails Create(Report report) => new(
        report.Id,
        report.Title.Value,
        string.IsNullOrWhiteSpace(report.Description) ? localizer.GetString("Details_NoDescription") : report.Description,
        report.Priority, formatter.FormatPriority(report.Priority),
        report.Status, formatter.FormatStatus(report.Status),
        formatter.FormatDate(report.CreatedAt), formatter.FormatDate(report.UpdatedAt),
        report.SyncState == SyncState.Pending, formatter.FormatSyncState(report.SyncState),
        report.PhotoPath,
        report.AllowedNextStatuses.Select(s => new StatusAction(s, localizer.GetString($"StatusAction_{s}"))).ToList());
}
