using FieldReport.Core.Services;

namespace FieldReport.App.Pages;

public partial class ReportDetailsPage : BasePage
{
    public ReportDetailsPage(IErrorHandler errors) : base(errors)
    {
        InitializeComponent();
    }
}
