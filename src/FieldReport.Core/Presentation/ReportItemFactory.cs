using FieldReport.Core.Models;

namespace FieldReport.Core.Presentation;

/// <summary>Jedna odpowiedzialność: Report → ReportItem.</summary>
// TODO: dodaj parametr konstruktora IReportFormatter formatter: ReportItemFactory(IReportFormatter formatter)
public sealed class ReportItemFactory : IReportItemFactory
{
    // TODO: zbuduj ReportItem: teksty z formattera, IsPendingSync = report.SyncState == SyncState.Pending
    public ReportItem Create(Report report) => throw new NotImplementedException();
}
