using FieldReport.Core.Data;
using FieldReport.Core.Data.Api;
using FieldReport.Core.Models;

namespace FieldReport.Core.Services.Sync;

/// <summary>
/// Wysyłka zgłoszeń czekających w telefonie (offline i synchronizacja). Nowe zgłoszenie: POST z kluczem idempotencji zapisanym w telefonie
/// (ponowiona wysyłka nie tworzy duplikatu), zmiana zgłoszenia z serwera: PUT; potem zdjęcie.
/// Po sukcesie zgłoszenie i plik zdjęcia znikają z telefonu.
/// Błąd sieci przerywa wysyłkę: reszta zostaje w telefonie i pójdzie przy następnej synchronizacji.
/// </summary>
public sealed class SyncService(
    IReportRepository local, IReportsApiClient api, IConnectivityService connectivity, IPhotoStorage photos) : ISyncService
{
    // Jedna synchronizacja naraz w całej aplikacji (lista, zapis, powrót z tła) —
    // druga czeka, zamiast wysyłać to samo
    private static readonly SemaphoreSlim Gate = new(1, 1);

    public async Task<Result> SyncAsync(CancellationToken ct = default)
    {
        // bez sieci nawet nie próbujemy
        if (!connectivity.IsOnline) return Result.Failure(Errors.Network);

        await Gate.WaitAsync(ct);
        try
        {
            // w SQLite są tylko niewysłane
            foreach (var report in await local.GetAllAsync(ct))
            {
                var sent = await SendAsync(report, ct);
                if (!sent.IsSuccess) return sent;                // reszta poczeka
            }
            return Result.Success();
        }
        finally
        {
            Gate.Release();
        }
    }

    private async Task<Result> SendAsync(Report report, CancellationToken ct)
    {
        if (report.IsOnServer)
        {
            // zgłoszenie jest już na serwerze (np. zmiana statusu): PUT na jego Id
            var updated = await api.UpdateAsync(ReportDtoMapper.ToDto(report), ct);
            if (!updated.IsSuccess) return updated;
        }
        else
        {
            // nowe zgłoszenie: POST z kluczem zapisanym w telefonie — ten sam klucz przy każdej próbie,
            // więc po zgubionej odpowiedzi serwer odda to samo zgłoszenie zamiast tworzyć drugie
            var created = await api.CreateAsync(ReportDtoMapper.ToDto(report), report.IdempotencyKey!.Value, ct);
            if (!created.IsSuccess) return Result.Failure(created.Error!);

            // Id nadał serwer: zapisujemy zgłoszenie pod tym Id jako „na serwerze” (zdjęcie może się nie wysłać —
            // wtedy następna synchronizacja zrobi PUT i wyśle zdjęcie, bez drugiego POST). Stary wiersz znika w tej samej
            // transakcji: lista widzi stan przed albo po, nigdy oba wiersze, a po awarii aplikacji nic nie ginie
            var localId = report.Id;
            report.MarkOnServer(created.Value!.Id);
            await local.ReplaceAsync(localId, report, ct);

            // Klucz dotyczy utworzenia, więc serwer porównuje tylko tytuł, opis, priorytet i datę utworzenia — nie status.
            // Gdy odpowiedź na poprzedni POST zginęła, a technik w tym czasie zmienił status, serwer odda zgłoszenie
            // ze starym statusem: nowy wysyłamy zwykłym PUT (nieudany PUT ponowi następna synchronizacja)
            if (created.Value.Status != report.Status)
            {
                var updated = await api.UpdateAsync(ReportDtoMapper.ToDto(report), ct);
                if (!updated.IsSuccess) return updated;
            }
        }

        if (report.PhotoPath is not null)
        {
            var photo = await api.UploadPhotoAsync(report.Id, report.PhotoPath, ct); // Id z serwera
            if (!photo.IsSuccess) return photo; // spróbujemy przy następnej synchronizacji
            await photos.DeleteAsync(report.PhotoPath, ct);
        }

        await local.DeleteAsync(report.Id, ct); // wysłane: usuwamy z telefonu
        return Result.Success();
    }
}
