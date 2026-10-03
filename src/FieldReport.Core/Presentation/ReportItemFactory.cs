using FieldReport.Core.Models;

namespace FieldReport.Core.Presentation;

/// <summary>Jedna odpowiedzialność: Report → ReportItem.</summary>
public sealed class ReportItemFactory(IReportFormatter formatter) : IReportItemFactory
{
    public ReportItem Create(Report report) => new(
        report.Id,
        report.Title.Value,                          // ReportTitle → zwykły tekst
        formatter.FormatDate(report.CreatedAt),      // data jako tekst
        report.Priority, formatter.FormatPriority(report.Priority),
        report.Status, formatter.FormatStatus(report.Status),
        report.SyncState == SyncState.Pending,       // „Oczekuje na wysłanie”
        report.PhotoPath,
        report.CanDelete);
}
