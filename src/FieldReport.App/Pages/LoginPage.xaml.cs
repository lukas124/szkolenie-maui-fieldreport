using FieldReport.Core.Services;

namespace FieldReport.App.Pages;

public partial class LoginPage : BasePage
{
    public LoginPage(IErrorHandler errors) : base(errors)
    {
        InitializeComponent();
    }
}
