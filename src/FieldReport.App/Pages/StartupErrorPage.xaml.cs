using FieldReport.Core.Services;

namespace FieldReport.App.Pages;

public partial class StartupErrorPage : BasePage
{
    public StartupErrorPage(IErrorHandler errors) : base(errors)
    {
        InitializeComponent();
    }
}
