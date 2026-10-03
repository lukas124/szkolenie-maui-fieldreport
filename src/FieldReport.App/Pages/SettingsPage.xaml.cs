using FieldReport.Core.Services;

namespace FieldReport.App.Pages;

public partial class SettingsPage : BasePage
{
    public SettingsPage(IErrorHandler errors) : base(errors)
    {
        InitializeComponent();
    }
}
