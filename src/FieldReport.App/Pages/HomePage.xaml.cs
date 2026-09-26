namespace FieldReport.App.Pages;

public partial class HomePage : BasePage
{
    public HomePage()
    {
        InitializeComponent();
        ActionCommand = new Command(OpenSettings); // zębatka w nagłówku
    }

    // Dotknięcie zębatki obsłużone w code-behind: otwiera Ustawienia na stosie nawigacji
    private async void OpenSettings()
    {
        try
        {
            await Navigation.PushAsync(new SettingsPage());
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex); // async void zawsze z try/catch
        }
    }
}
