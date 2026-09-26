# Ćwiczenie 02 — Lista zgłoszeń w MVVM

**Blok:** 02 · MVVM i ViewModel zgodny z SRP · dzień 1 · **Start:** gałąź `02-mvvm-start` · **Czas:** 20 min

## Cel
Lista zgłoszeń z ViewModelem, który **wyłącznie orkiestruje**: usługa → fabryka → widok / komunikat.

## Co jest w gałęzi startowej
- `Report`, `IReportService` z danymi przykładowymi (`SampleReportService`), `ReportItem` (gotowe teksty dla widoku),
  interfejsy `IReportFormatter`, `IReportItemFactory`, `IUserFeedback`
- `BaseViewModel`, `BasePage`, strona `ReportsPage` z prostym szablonem listy, teksty w `Strings.resx` i `Strings.en.resx`
- `App.CreateWindow` składa obiekty ręcznie, w jednym miejscu: usługi (`MauiDialogService`, `LoggingErrorHandler`,
  `NavigationService`, `SampleReportService`…) → ViewModel → strona. Kontener DI MAUI trzyma tylko `ILocalizer`,
  bo potrzebuje go XAML (`{loc:Translate}`); resztę przejmie kontener w bloku 04
- `ReportFormatter`, `ReportItemFactory` i `ReportsViewModel` bez zależności, których jeszcze nie używają —
  parametry konstruktora dopisujesz razem z kodem, który ich potrzebuje

## Kroki
Przejdź na gałąź startową ćwiczenia. Trener udostępnia gałęzie stopniowo, więc najpierw `git fetch` pobiera te,
które właśnie się pojawiły (masz lokalne zmiany i Git nie pozwala zmienić gałęzi? Najpierw `git stash`):

```bash
# pobierz nowe gałęzie i przejdź na start ćwiczenia
git fetch && git checkout 02-mvvm-start
```

W gałęzi startowej miejsca do uzupełnienia są oznaczone `TODO:` (lista: Task List w Visual Studio, TODO w Riderze
albo `git grep -n "TODO:"`). Testy rozwiązania mają atrybut `[Ignore]` — usuń go, gdy uzupełnisz TODO.

1. **Formatter** `Core/Presentation/ReportFormatter` (TODO): dodaj parametr konstruktora `ILocalizer localizer`
   (`public sealed class ReportFormatter(ILocalizer localizer) : IReportFormatter`), a potem metody:
   - `FormatDate(DateTimeOffset)` → `value.ToLocalTime().ToString("g", localizer.CurrentCulture)`
   - `FormatStatus` / `FormatPriority` → tekst z zasobów (`ReportStatus_{status}`, `ReportPriority_{priority}`) przez `localizer.GetString(...)`.
2. **Fabryka** `ReportItemFactory` (TODO): parametr `IReportFormatter formatter`, potem `Create` — jedna odpowiedzialność:
   `Report → ReportItem` (teksty z formattera, `IsPendingSync = SyncState == Pending`).
3. **ViewModel** `ReportsViewModel` (TODO): parametry `IReportService reports`, `IReportItemFactory items`, `IUserFeedback feedback`
   (obok `INavigationService navigation`), potem `OnNavigatedToAsync`: `reports.GetAllAsync()` → błąd:
   `feedback.ShowErrorAsync(result.Error!)`; sukces: `Reports.ReplaceWith(result.Value!.Select(items.Create))`.
4. `App.CreateWindow` (TODO): przekaż nowe zależności — `new ReportItemFactory(new ReportFormatter(_localizer))`,
   `new ReportsViewModel(navigation, reports, items, feedback)`. ViewModel dostaje usługi w konstruktorze i nie wie, skąd pochodzą.
   W XAML teksty przez rozszerzenie znaczników: `Title="{loc:Translate Reports_Title}"`, w kodzie Core — `ILocalizer.GetString(...)`.
5. **Testy:** w `SetUp` obu klas testów przekaż zależności do konstruktorów (TODO), usuń `[Ignore]` z `ReportItemFactoryTests`
   i `ReportsViewModelTests`, uruchom `dotnet test`:
   - niewysłane zgłoszenie → `IsPendingSync == true`
   - błąd usługi → `ShowErrorAsync` wywołane **raz** (`Times.Once`)

## Kryteria akceptacji
- Po starcie widać listę 6 przykładowych zgłoszeń, od najnowszej aktualizacji.
- Daty w formacie polskim (np. `26.09.2026 13:52`), statusy i priorytety po polsku (z `Strings.resx`).
- ViewModel nie formatuje dat, nie mapuje modeli i nie zna typów MAUI (`Core` nie referuje MAUI).
- `dotnet test` na zielono (testy bez `[Ignore]`).

## Podpowiedzi
- Pusty stan pokazuje sam widok (`CollectionView.EmptyView`, blok 03) — ViewModel nie potrzebuje do tego osobnej flagi.
- `List<T>` nie powiadamia widoku o zmianach — używaj `ObservableCollection<T>`.
- Ładowanie w `OnNavigatedToAsync`, a nie w `OnAppearing` (to drugie odpala się też po zamknięciu modala).
- Mock `ILocalizer`: `localizer.Setup(l => l.GetString(It.IsAny<string>())).Returns((string k) => k);` (albo gotowy `TestData/KeyLocalizer`).
- Drugi język: `localizer.SetLocale(new CultureInfo("en"))` przełącza teksty w locie — `{loc:Translate}` to Binding do `Localizer`, który po zmianie języka zgłasza `PropertyChanged`.
- „Ile przypadków testowych ma ViewModel?” — dwa: sukces i błąd. Formatowanie i mapowanie mają własne, proste testy.

## Jeśli utkniesz
```bash
# odłóż swoje zmiany (git stash), pobierz nowe gałęzie (git fetch) i przejdź do gotowego rozwiązania
git stash && git fetch && git checkout 02-mvvm
```
