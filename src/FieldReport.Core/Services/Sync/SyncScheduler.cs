namespace FieldReport.Core.Services.Sync;

/// <summary>
/// Uruchamia synchronizację w tle i nie czeka na nią (fire-and-forget). Błąd w tle trafia do logów — dane i tak
/// czekają w telefonie. Listę odświeża jej ViewModel (powrót na listę, pull-to-refresh).
/// SyncService jest transient (używa HttpClient z fabryki), dlatego tworzymy go przy każdym zleceniu.
/// </summary>
public sealed class SyncScheduler(Func<ISyncService> syncFactory, IErrorHandler errors) : ISyncScheduler
{
    public void RequestSync() => _ = RunAsync();

    private async Task RunAsync()
    {
        try
        {
            await syncFactory().SyncAsync();
        }
        catch (Exception ex)
        {
            errors.Handle(ex);
        }
    }
}
