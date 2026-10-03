using FieldReport.Core.Services;

namespace FieldReport.App.Pages;

public partial class ReportsPage : BasePage
{
    public ReportsPage(IErrorHandler errors) : base(errors)
    {
        InitializeComponent();
    }
}
