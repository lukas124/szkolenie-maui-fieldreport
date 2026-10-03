# Ćwiczenie 12 — Lista z API i synchronizacja

**Blok:** 12 · REST API i synchronizacja · dzień 2 · **Start:** gałąź `12-api-start` · **Czas:** 20 min

## Cel
Poprawna, odporna komunikacja HTTP: typed client + `System.Text.Json`, błędy HTTP i brak sieci jako `Result`,
bezpieczne ponawianie, wysyłka jako proces w usłudze (nie w ViewModelu).

## Model danych
- **Serwer (API) jest źródłem prawdy.** Lista = zgłoszenia z API + zgłoszenia czekające w telefonie na wysłanie.
- **SQLite trzyma tylko niewysłane**: nowe zgłoszenia i zmiany statusu (+ ścieżki ich zdjęć). Po udanej wysyłce
  zgłoszenie i plik zdjęcia znikają z telefonu.
- **Offline** lista pokazuje tylko niewysłane i pasek „Brak połączenia z serwerem…” (pusta lista: „Brak zgłoszeń czekających na wysłanie.”).
- Zdjęcie zgłoszenia z serwera: `photoUrl` z API to **podpisany adres** (ważny ograniczony czas), więc wystarczy
  `Image Source="{Binding ThumbnailSource}"` — bez tokenu i bez kopiowania pliku do telefonu.

## Co jest w gałęzi startowej
- Z `11-push`: logowanie przez API (`ApiAuthService`, `AuthApiClient`), `AuthHeaderHandler` (Bearer, 401 → odświeżenie
  tokenu → jedno ponowienie, nieudane → logowanie), `HttpResultExtensions` (jedna metoda `CatchErrorsAsync` zamienia
  wyjątki na `Result`), `DevicesApiClient`.
- `Core/Data/Api`: `ReportDto` (kontrakt API, enumy jako tekst, `photoUrl`), `ReportDtoMapper`, `IReportsApiClient`/`ReportsApiClient`
  (`GetReportsAsync`, `GetReportAsync(id)` — do napisania, `CreateAsync(report, idempotencyKey)` = `POST api/reports` z nagłówkiem
  `Idempotency-Key` (Id nadaje serwer), `UpdateAsync` = `PUT api/reports/{id}`, `UploadPhotoAsync` = multipart `photo`).
- `HttpResultExtensions` łapie też wyjątki Polly (`ExecutionRejectedException`: `TimeoutRejectedException`,
  `BrokenCircuitException`) → `Errors.Network`.
- `ReportService`: `GetAllAsync()` → `ReportList(Reports, IsOffline)`; `GetAsync(id)` najpierw z telefonu, potem z API;
  `CreateAsync`/`ChangeStatusAsync` zapisują w telefonie i zlecają wysyłkę w tle.
- `Core/Services/Sync`: `ISyncService`/`SyncService` (wysyłka jednego zgłoszenia `SendAsync` gotowa: nowe → `POST` z kluczem
  idempotencji, zgłoszenie dostaje Id z serwera; zmiana zgłoszenia z serwera → `PUT`; potem zdjęcie → usunięcie z telefonu),
  `ISyncScheduler`/`SyncScheduler` (wysyłka w tle po zapisie, po starcie aplikacji, po `Window.Resumed` i po powrocie internetu), `IConnectivityService`.
- Repozytorium: `DeleteAllAsync`, `ReplaceAsync` (po `POST` stary wiersz znika, a zgłoszenie z Id z serwera
  zapisuje się w jednej transakcji — lista nigdy nie widzi obu wierszy);
  migracja 2 (kolumna `IsOnServer` — zgłoszenie z serwera ze zmianą statusu czeka w telefonie),
  migracja 3 (kolumna `IdempotencyKey` — klucz generowany **raz** w `Report.CreateNew`, zapisany w wierszu niewysłanego
  zgłoszenia i wysyłany przy każdej próbie, także po restarcie aplikacji).
- Lista: `RefreshView` + `RefreshCommand` (ciało do napisania), pasek „offline” (`IsOffline`).
- Wylogowanie: ostrzeżenie o niewysłanych → wyrejestrowanie urządzenia → usunięcie sesji, niewysłanych zgłoszeń i zdjęć.
- Rejestracja: `AddHttpClient<IReportsApiClient, ReportsApiClient>(…).AddHttpMessageHandler<AuthHeaderHandler>()
  .AddStandardResilienceHandler(o => o.Retry.DisableForUnsafeHttpMethods())`.
- HTTP bez TLS tylko w Debug (`[Application(UsesCleartextTraffic = true)]` pod `#if DEBUG`), iOS: `NSAllowsLocalNetworking`.

## Kroki
Przejdź na gałąź startową ćwiczenia. Trener udostępnia gałęzie stopniowo, więc najpierw `git fetch` pobiera te,
które właśnie się pojawiły (masz lokalne zmiany i Git nie pozwala zmienić gałęzi? Najpierw `git stash`):

```bash
# pobierz nowe gałęzie i przejdź na start ćwiczenia
git fetch && git checkout 12-api-start
```

W gałęzi startowej miejsca do uzupełnienia są oznaczone `TODO:` (lista: Task List w Visual Studio, TODO w Riderze
albo `git grep -n "TODO:"`). Testy rozwiązania mają atrybut `[Ignore]` — usuń go, gdy uzupełnisz TODO.

1. `ReportsApiClient.GetReportAsync` (TODO; ścieżki **względne**, bez `/` na początku), wzór — `GetReportsAsync`:
   ```csharp
   public Task<Result<IReadOnlyList<ReportDto>>> GetReportsAsync(CancellationToken ct = default) =>
       http.SendForResultAsync<IReadOnlyList<ReportDto>>(new HttpRequestMessage(HttpMethod.Get, "api/reports"), ct);
   ```
2. `SyncService` (TODO): dodaj parametr konstruktora `IConnectivityService connectivity` (w `SetUp` testów przekaż mock — TODO),
   potem `SyncAsync`: offline → `Errors.Network`; pod blokadą `Gate` dla każdego zgłoszenia z telefonu
   `SendAsync` (pierwszy błąd przerywa wysyłkę i jest wynikiem).
3. Obejrzyj rejestrację typed clienta z `AuthHeaderHandler` i `AddStandardResilienceHandler` (`AddApiClients`).
4. `ReportsViewModel` (TODO): parametr `ISyncService sync` (w `SetUp` testów przekaż mock — TODO), potem `RefreshCommand`: `await sync.SyncAsync(ct)` → `await LoadAsync()`, `IsRefreshing = false` w `finally`
   (lista pokaże pasek „offline”, gdy serwer nie odpowiada).
5. **Testy:** usuń `[Ignore]` z `SyncServiceTests`, `ReportsApiClientTests.GetReport_UsesIdInPath` i testów `Refresh_*`
   w `ReportsViewModelTests` (m.in. offline → `Errors.Network`, zgłoszenie zostaje w telefonie).

## Kryteria akceptacji
- Po zalogowaniu lista pobiera zgłoszenia technika z API (też te od dyspozytora, ze zdjęciem — miniatura z `photoUrl`).
- Zgłoszenie z kreatora (ze zdjęciem) trafia do API (`GET /api/dispatcher/reports`: `hasPhoto: true`), a zgłoszenie i plik
  zdjęcia znikają z telefonu; po odświeżeniu listy (pull-to-refresh albo powrót na listę) znacznik „Oczekuje na wysłanie” znika.
- Tryb samolotowy: lista pokazuje tylko niewysłane i pasek o braku połączenia; zmiana statusu offline → „Oczekuje na wysłanie”;
  po powrocie sieci (albo pull-to-refresh, powrót aplikacji z tła, ponowne uruchomienie) zmiana trafia do API.
- Push dyspozytora (tło / zamknięta aplikacja) → tapnięcie → szczegóły nowego zgłoszenia (pobrane z API);
  pierwszy plan → baner + zgłoszenie na liście.
- Ponowiona wysyłka po zgubionej odpowiedzi nie tworzy duplikatu (`POST` z tym samym `Idempotency-Key` → serwer zwraca
  to samo zgłoszenie): na serwerze jedno zgłoszenie (`GET /api/dispatcher/reports`).
- Po 10 min (ważność access tokenu) aplikacja działa dalej (odświeżenie tokenu); nieudane odświeżenie → logowanie.
- Wylogowanie z niewysłanymi → ostrzeżenie „Po wylogowaniu zostaną usunięte z telefonu”.
- Testy, z których usunięto `[Ignore]`, przechodzą (`dotnet test` na zielono).

## Podpowiedzi
- `IHttpClientFactory`, a nie `new HttpClient()`; typed client jest transient — nie trzymamy go w singletonie
  (dlatego `SyncScheduler` tworzy `SyncService` z fabryki `Func<ISyncService>` przy każdym zleceniu).
- Lista odświeża się w jednym miejscu: `ReportsViewModel.OnAppearingAsync` (powrót ze szczegółów, z kreatora, z Ustawień).
  `SyncScheduler` nie wysyła komunikatu — inaczej po zmianie statusu lista pobierałaby API dwa razy.
- Retry tylko dla bezpiecznych metod (GET): zapis po zgubionej odpowiedzi jest niejednoznaczny — stąd nagłówek
  `Idempotency-Key` (standard IETF) przy `POST`: serwer pamięta klucz i przy powtórce oddaje zapisany wynik
  (ten sam klucz z inną treścią → `422`, równoległe żądanie z tym samym kluczem → `409`). Ponowienie zapisu = kolejna synchronizacja.
- Każdy zapis najpierw trafia do telefonu, dopiero potem do API — dlatego aplikacja działa w piwnicy bez zasięgu.
- Emulator → API na laptopie: wpisz `http://10.0.2.2:5080/` w `ApiSettings.TrainingBaseUrl` (HTTP bez TLS działa tylko w Debug);
  symulator iOS → `http://localhost:5080/`.
- Klient HTTP piszemy ręcznie, żeby zobaczyć każde żądanie; w projektach generuje się go z opisu OpenAPI (np. NSwag).

## Jeśli utkniesz
```bash
# odłóż swoje zmiany (git stash), pobierz nowe gałęzie (git fetch) i przejdź do gotowego rozwiązania
git stash && git fetch && git checkout 12-api
```
