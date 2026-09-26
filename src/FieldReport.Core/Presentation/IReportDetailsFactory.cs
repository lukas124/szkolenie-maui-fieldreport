using FieldReport.Core.Models;

namespace FieldReport.Core.Presentation;

public interface IReportDetailsFactory
{
    ReportDetails Create(Report report);
}
