using FieldReport.Core.Services;

namespace FieldReport.App.Pages;

public partial class PlaygroundPage : BasePage
{
    public PlaygroundPage(IErrorHandler errors) : base(errors)
    {
        InitializeComponent();
    }

    // Źle — blokuje wątek UI: aplikacja „zamarza”
    // (przy dłuższym czasie Android pokaże ANR)
    private void OnBlockingClicked(object? sender, EventArgs e)
    {
        // ten tekst nawet się nie narysuje — wątek UI śpi
        UiThreadStatus.Text = "Zamrożone…";
        Thread.Sleep(3000);
        UiThreadStatus.Text = "Thread.Sleep: UI było zablokowane przez 3 s.";
    }

    // Dobrze — await oddaje wątek UI: animacje i przewijanie działają
    private async void OnAwaitClicked(object? sender, EventArgs e)
    {
        try
        {
            UiThreadStatus.Text = "Czekam… (UI działa, możesz przewijać)";
            await Task.Delay(3000);
            UiThreadStatus.Text = "await: UI przez cały czas reagowało.";
        }
        catch (Exception ex)
        {
            // async void tylko w handlerach i zawsze z try/catch
            System.Diagnostics.Debug.WriteLine(ex);
        }
    }
}
