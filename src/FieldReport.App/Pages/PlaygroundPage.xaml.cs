using FieldReport.Core.Services;

namespace FieldReport.App.Pages;

public partial class PlaygroundPage : BasePage
{
    public PlaygroundPage(IErrorHandler errors) : base(errors)
    {
        InitializeComponent();
    }
}
