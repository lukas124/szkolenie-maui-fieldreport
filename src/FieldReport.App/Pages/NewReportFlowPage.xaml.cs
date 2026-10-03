using FieldReport.Core.Services;

namespace FieldReport.App.Pages;

public partial class NewReportFlowPage : BasePage
{
    public NewReportFlowPage(IErrorHandler errors) : base(errors)
    {
        InitializeComponent();
    }
}
