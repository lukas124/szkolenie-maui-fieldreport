namespace FieldReport.Core.Services.Sync;

/// <summary>Synchronizacja w tle „na zlecenie” (po zapisie, po powrocie z tła, po powrocie sieci). Nie blokuje UI.</summary>
public interface ISyncScheduler
{
    void RequestSync();
}
