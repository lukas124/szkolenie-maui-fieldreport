using FieldReport.Core.Models;

namespace FieldReport.Core.Presentation;

public interface IReportItemFactory
{
    ReportItem Create(Report report);
}
