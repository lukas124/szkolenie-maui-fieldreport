namespace FieldReport.App.Pages;

/// <summary>Dialog jako strona modalna. Result kończy się, gdy użytkownik zamknie dialog.</summary>
public partial class DialogPage : ContentPage
{
    // TaskCompletionSource: zadanie, które kończymy sami — w chwili dotknięcia przycisku
    private readonly TaskCompletionSource<bool> _result = new();
    private bool _closing;

    public DialogPage(string title, string message, string accept)
    {
        InitializeComponent();
        TitleLabel.Text = title;
        MessageLabel.Text = message;
        AcceptButton.Text = accept;
    }

    /// <summary>Kończy się po zamknięciu dialogu (true = przycisk akceptacji).</summary>
    public Task<bool> Result => _result.Task;

    private void OnAcceptClicked(object? sender, EventArgs e) => _ = CloseAsync(true);

    // Android: systemowy przycisk „wstecz” też zamyka dialog
    protected override bool OnBackButtonPressed()
    {
        _ = CloseAsync(false);
        return true;
    }

    private async Task CloseAsync(bool result)
    {
        if (_closing) return; // drugie dotknięcie w trakcie zamykania
        _closing = true;
        try
        {
            await Navigation.PopModalAsync(false);
        }
        finally
        {
            // dopiero po zamknięciu dialogu (także nieudanym): kod czekający
            // na wynik może od razu nawigować, a MauiDialogService nie zostaje
            // zablokowany na zawsze
            _result.TrySetResult(result);
        }
    }
}
