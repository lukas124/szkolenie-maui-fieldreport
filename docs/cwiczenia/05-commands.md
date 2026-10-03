# Ćwiczenie 05 — Usuwanie z potwierdzeniem

**Blok:** 05 · Komendy, async/await i wątek UI · dzień 1 · **Start:** gałąź `05-commands-start` · **Czas:** 10 min

## Cel
Usuwanie niewysłanego zgłoszenia przesunięciem, zawsze z potwierdzeniem. Komenda jako scenariusz orkiestracji:
**potwierdź → wykonaj (usługa) → pokaż wynik → zaktualizuj widok**.

## Co jest w gałęzi startowej
- `IReportService.DeleteAsync(Guid id, CancellationToken ct)` → `Result` (implementacja w `ReportService` do napisania),
  błędy `Errors.ReportAlreadySynced`, `Errors.ReportNotFound` i ich teksty w `Strings.resx`
- `IUserFeedback.ConfirmAsync(titleKey, messageKey)` → `IDialogService.ConfirmAsync(...)` z tekstami z zasobów
- `ReportsPage.xaml`: karta w `SwipeView` z akcją „Usuń” (`DeleteReportCommand` przez `RelativeSource AncestorType`,
  `CommandParameter="{Binding .}"`, widoczna tylko dla niewysłanych)
- `ReportsViewModel`: komenda `DeleteReportAsync` z `CanExecute = nameof(CanDelete)` (ciało do napisania)

## Kroki
Przejdź na gałąź startową ćwiczenia. Trener udostępnia gałęzie stopniowo, więc najpierw `git fetch` pobiera te,
które właśnie się pojawiły (masz lokalne zmiany i Git nie pozwala zmienić gałęzi? Najpierw `git stash`):

```bash
# pobierz nowe gałęzie i przejdź na start ćwiczenia
git fetch && git checkout 05-commands-start
```

W gałęzi startowej miejsca do uzupełnienia są oznaczone `TODO:` (lista: Task List w Visual Studio, TODO w Riderze
albo `git grep -n "TODO:"`). Testy rozwiązania mają atrybut `[Ignore]` — usuń go, gdy uzupełnisz TODO.

1. `ReportService.DeleteAsync` (TODO) — reguła „tylko niewysłane”: brak zgłoszenia → `Errors.ReportNotFound`,
   wysłane (`SyncState != Pending`) → `Errors.ReportAlreadySynced`, w przeciwnym razie `repository.DeleteAsync`.
2. `ReportsViewModel.DeleteReportAsync` (TODO) — scenariusz komendy:
   ```csharp
   if (!await feedback.ConfirmAsync("Reports_DeleteTitle", "Reports_DeleteMessage")) return;
   var result = await reports.DeleteAsync(item.Id, ct);
   if (!result.IsSuccess) { await feedback.ShowErrorAsync(result.Error!); return; }
   Reports.Remove(item);
   ```
   (metoda z `[RelayCommand]` staje się `async Task`).
3. **Testy:** usuń `[Ignore]` z testów `DeleteReport_*` w `ReportsViewModelTests` i `Delete_*` w `ReportServiceTests`:
   - brak potwierdzenia → usługa niewywołana (`Times.Never`)
   - błąd usługi → `ShowErrorAsync` raz, element zostaje na liście

## Kryteria akceptacji
- Przesunięcie niewysłanego zgłoszenia w lewo pokazuje czerwoną akcję „Usuń”; wysłanego — nic.
- „Usuń” zawsze pyta „Usunąć zgłoszenie?”; „Nie” nic nie zmienia, „Tak” usuwa kartę z listy.
- Usunięcie ostatniego zgłoszenia pokazuje pusty stan. Wysłanych nie usuniesz (reguła), więc do pustej listy dojdziesz
  przez Playground (zębatka → Ustawienia → Playground) → „Wyczyść listę”; „Przywróć przykłady” wraca do 6 zgłoszeń.
- `dotnet test` kończy się na zielono.

## Demo: wątek UI (Playground → „// wątek UI”)
- `Thread.Sleep(3000)` → zamrożona aplikacja (przy dłuższym czasie ANR); `await Task.Delay(3000)` → UI żyje.
- `.Result` / `.Wait()` na wątku UI → zakleszczenie. `async void` tylko w handlerach i zawsze z try/catch.
- `MainThread.BeginInvokeOnMainThread` / `InvokeOnMainThreadAsync` — aktualizacja widoku z wątku w tle.
- `ConfigureAwait(false)` jest dla bibliotek bez UI. W kodzie aplikacji (ViewModel, strona, także Core) zostaw domyślne
  `ConfigureAwait(true)`: po `await` kod wraca na wątek UI, więc wolno mu zmieniać widok.

## Podpowiedzi
- Metoda `…Async` z `[RelayCommand]` → `AsyncRelayCommand`: blokuje podwójne wykonanie (`IsRunning`), przyjmuje `CancellationToken`.
- Przewidywalny błąd (np. „zgłoszenie już wysłane”) to `Result`, a nie wyjątek.
- `ReportItem` to `record` — `Reports.Remove(item)` porównuje po wartości.
- Test komendy: `await _vm.DeleteReportCommand.ExecuteAsync(item);`

## Jeśli utkniesz
```bash
# odłóż swoje zmiany (git stash), pobierz nowe gałęzie (git fetch) i przejdź do gotowego rozwiązania
git stash && git fetch && git checkout 05-commands
```
