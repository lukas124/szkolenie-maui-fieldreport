using FieldReport.Core.Models;

namespace FieldReport.Core.Services.Sync;

/// <summary>Synchronizacja (praca offline): wysłanie zgłoszeń i zmian czekających w telefonie, potem usunięcie ich z telefonu.</summary>
public interface ISyncService
{
    Task<Result> SyncAsync(CancellationToken ct = default);
}
