using FieldReport.Core.Services;

namespace FieldReport.App.Pages;

public partial class StartupPage : BasePage
{
    public StartupPage(IErrorHandler errors) : base(errors)
    {
        InitializeComponent();
    }
}
