# Ćwiczenie 13 — Testy kreatora i synchronizacji

**Blok:** 13 · Testy i debugowanie · dzień 2 · **Start:** gałąź `13-tests-start` · **Czas:** 10 min

## Cel
Testy jednostkowe (NUnit + Moq, `net10.0`, bez emulatora) dla ViewModeli, usług, modeli i formatterów.
ViewModel zgodny z SRP = proste testy **orkiestracji**; reguły testujemy w modelu, teksty w formatterze.

| Co | Charakter testu | Przykłady w repo |
|---|---|---|
| modele (`ReportTitle`, `Report.ChangeStatus`, `Report.CanDelete`) | czyste, dużo przypadków brzegowych, bez mocków | `Models/` |
| usługi (`SyncService`, `ReportService`, `ApiAuthService`) | mocki repozytorium / API (albo prawdziwe in-memory) | `Services/`, `Services/Sync/` |
| fabryki, formatery, `StepSequence`, `PushMessageRouter` | czyste | `Presentation/`, `ViewModels/NewReport/`, `Services/Push/` |
| ViewModele | tylko orkiestracja: co wywołano, jaki stan widoku, gdzie nawigacja | `ViewModels/` |
| dane | prawdziwy SQLite w katalogu tymczasowym, klienci HTTP z atrapą `HttpMessageHandler` | `Data/`, `Data/Api/` |
| reguły architektury | refleksja: Core bez MAUI, ≤ 6 zależności ViewModelu, teksty w zasobach | `Architecture/` |

## Kroki
Przejdź na gałąź startową ćwiczenia. Trener udostępnia gałęzie stopniowo, więc najpierw `git fetch` pobiera te,
które właśnie się pojawiły (masz lokalne zmiany i Git nie pozwala zmienić gałęzi? Najpierw `git stash`):

```bash
# pobierz nowe gałęzie i przejdź na start ćwiczenia
git fetch && git checkout 13-tests-start
```

W gałęzi startowej testy do napisania mają ciało z `TODO:` i atrybut `[Ignore]` — usuń go, gdy napiszesz test.

1. **Kreator** (`ViewModels/NewReport/NewReportFlowViewModelTests.cs`, gotowy `SetUp`, dwa testy z TODO):
   ```csharp
   [SetUp]
   public void SetUp()
   {
       _navigation = new Mock<INavigationService>();
       _reports = new Mock<IReportService>();
       _feedback = new Mock<IUserFeedback>();
       _details = new DetailsStepViewModel(Mock.Of<IErrorMessageProvider>(), Mock.Of<ILocalizer>());
       _vm = new NewReportFlowViewModel(_navigation.Object, _reports.Object, _feedback.Object,
           _details, new PhotoStepViewModel(Mock.Of<IMediaService>(), Mock.Of<IPermissionService>(), Mock.Of<IUserFeedback>(), Mock.Of<ILocalizer>()),
           new SummaryStepViewModel(Mock.Of<IReportFormatter>(), Mock.Of<ILocalizer>()));
   }

   [Test]
   public void Next_IsDisabled_UntilDetailsAreValid() { /* TitleInput = "Awaria pompy" → CanExecute true */ }

   [Test]
   public async Task Next_OnLastStep_SavesAndResetsToList()
   {
       // CreateAsync → Result<Guid>.Success; 3 × NextCommand.ExecuteAsync(null)
       // Verify: CreateAsync raz, ResetToAsync<ReportsViewModel>() raz
   }
   ```
2. **Synchronizacja offline** (`Services/Sync/SyncServiceTests.cs`, test `Offline_ReturnsNetwork_AndReportStaysOnPhone` z TODO): `IConnectivityService.IsOnline = false` →
   `SyncAsync()` zwraca `Errors.Network`, API nie jest wołane (`VerifyNoOtherCalls`), zgłoszenie zostaje w telefonie
   (repozytorium) i dalej „Oczekuje na wysłanie”.
3. Usuń `[Ignore]` z napisanych testów i uruchom `dotnet test` — cały zestaw (ponad 300 testów) trwa ok. sekundy.

## Co jest w gałęzi startowej
- `Architecture/ArchitectureTests` — `FieldReport.Core` nie referuje `Microsoft.Maui*`; każdy ViewModel ma ≤ 6 zależności
  i nie zależy od `Core/Data` ani `HttpClient`.
- `Architecture/ResourcesTests` — każdy kod `Errors.*` ma `Error_{kod}`, każdy status/priorytet/typ push ma tekst,
  każdy klucz użyty w XAML (`{loc:Translate …}`) i w kodzie istnieje w `Strings.resx`, a `Strings.en.resx` ma te same klucze.
- `SummaryStepViewModelTests`, `ReportRowMapperTests`, `ReportSyncStateTests` (`MarkOnServer`, zmiana statusu zgłoszenia
  z serwera); `TestData/InMemoryReportRepository` — repozytorium w pamięci tylko dla testów (`SyncServiceTests`).
- `AuthHeaderHandlerTests` (atrapa serwera z jednorazowym refresh tokenem: 401 → refresh → ponowienie, 5 równoległych 401
  = jedno odświeżenie, odrzucony refresh → logowanie), przejścia statusu wstecz / na ten sam, kolejność przy równych datach,
  podwójne tapnięcie komend.

## Kryteria akceptacji
- `dotnet test` na zielono, bez emulatora.
- Żaden test ViewModelu nie sprawdza reguły tytułu (to robi `ReportTitleTests`) — tylko orkiestrację.
- Test `SyncService` offline: `Errors.Network`, a zgłoszenie zostaje w telefonie.

## Debugowanie (demo trenera)
- Breakpoint (także warunkowy) na emulatorze/urządzeniu, „Break on exceptions”, Live Visual Tree / Live Property Explorer.
- Logi: `ILogger` + `AddDebug()`/`AddSimpleConsole()` w Debug → `adb logcat -s DOTNET`; iOS: `xcrun simctl spawn booted log stream --predicate 'process == "FieldReport.App"'`.
- Celowe błędy i objawy: `async void` bez try/catch (crash bez kontekstu), porzucony `Task` w `OnAppearing`
  („pusty ekran, zero logów”), `.Result` na wątku UI (ANR), aktualizacja widoku z wątku w tle (wyjątek), binding bez
  `x:DataType` (cisza) vs z `x:DataType` (błąd kompilacji `MAUIG2045`), subskrypcja zdarzenia singletona w ViewModelu
  (wyciek — w repo `WeakReferenceMessenger`).
- Produkcyjnie: telemetria (OpenTelemetry — ślady, metryki, logi) i raporty awarii (ang. crash reporting, np. Firebase
  Crashlytics); żeby raport z buildu Release pokazywał metodę i linię, wgrywasz symbole debugowania (`.pdb`, `mapping.txt`, dSYM).

## Jeśli utkniesz
```bash
# odłóż swoje zmiany (git stash), pobierz nowe gałęzie (git fetch) i przejdź do gotowego rozwiązania
git stash && git fetch && git checkout 13-tests
```
