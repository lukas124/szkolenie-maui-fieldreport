using FieldReport.Core.Models;
using FieldReport.Core.Services;

namespace FieldReport.Core.Presentation;

// TODO: dodaj parametr konstruktora ILocalizer localizer: ReportFormatter(ILocalizer localizer)
public sealed class ReportFormatter : IReportFormatter
{
    // TODO: data lokalna w kulturze użytkownika: ToLocalTime().ToString("g", localizer.CurrentCulture)
    public string FormatDate(DateTimeOffset value) => throw new NotImplementedException();

    // TODO: tekst z zasobów: localizer.GetString($"ReportStatus_{status}")
    public string FormatStatus(ReportStatus status) => throw new NotImplementedException();

    // TODO: tekst z zasobów: localizer.GetString($"ReportPriority_{priority}")
    public string FormatPriority(ReportPriority priority) => throw new NotImplementedException();
}
