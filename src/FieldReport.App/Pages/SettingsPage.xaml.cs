namespace FieldReport.App.Pages;

public partial class SettingsPage : BasePage
{
    public SettingsPage()
    {
        InitializeComponent();
    }

    // Przycisk „Playground”: otwiera stronę piaskownicy na stosie nawigacji
    private async void OnPlaygroundClicked(object? sender, EventArgs e)
    {
        try
        {
            await Navigation.PushAsync(new PlaygroundPage());
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex); // async void zawsze z try/catch
        }
    }
}
