# Ćwiczenie 07 — Z listy do szczegółów

**Blok:** 07 · Shell i nawigacja ViewModel-first · dzień 1 · **Start:** gałąź `07-navigation-start` · **Czas:** 10 min

## Cel
Lista → szczegóły z przekazaniem Id zgłoszenia; zmiana statusu, której reguła mieszka w modelu `Report`.

## Co jest w gałęzi startowej
- `Core/Navigation/INavigationService`: `NavigateToAsync<TViewModel>(object? parameter = null)` i `GoBackAsync()`
- `App/Navigation/PageRoutes` — zwykła mapa `Dictionary<Type, Type>` „ViewModel → strona”, wypełniana w `MauiProgram.CreatePageRoutes()`
  (parę dla szczegółów dopisujesz w kroku 4):
  ```csharp
  routes.Map<ReportsViewModel, ReportsPage>();
  routes.Map<PlaygroundViewModel, PlaygroundPage>();
  ```
- `App/Navigation/NavigationService`: bierze stronę i ViewModel z kontenera DI, wstawia stronę na stos, potem woła
  `viewModel.OnNavigatedToAsync(parameter)`. Pole `_isNavigating` blokuje drugą nawigację, dopóki trwa pierwsza.
- `RootPage : NavigationPage` (korzeń okna), `Report.AllowedNextStatuses` + `Report.ChangeStatus(next, now)` → `Result`
- `ReportDetailsViewModel` (zmiana statusu gotowa, wczytanie zgłoszenia do napisania), `ReportDetailsFactory`
  (akcje `StatusAction` z `AllowedNextStatuses`), strona `ReportDetailsPage` z `BindableLayout`
- `Report.IsOnServer` + reguła `Report.CanDelete` (usuwać można tylko nigdy niewysłane), `ReportItem.CanDelete`
- Konwertery ogólne z **CommunityToolkit.Maui** (`IsNotNullConverter` w `App.xaml`, `UseMauiCommunityToolkit()` w `MauiProgram`)

## Kroki
Przejdź na gałąź startową ćwiczenia. Trener udostępnia gałęzie stopniowo, więc najpierw `git fetch` pobiera te,
które właśnie się pojawiły (masz lokalne zmiany i Git nie pozwala zmienić gałęzi? Najpierw `git stash`):

```bash
# pobierz nowe gałęzie i przejdź na start ćwiczenia
git fetch && git checkout 07-navigation-start
```

W gałęzi startowej miejsca do uzupełnienia są oznaczone `TODO:` (lista: Task List w Visual Studio, TODO w Riderze
albo `git grep -n "TODO:"`). Testy rozwiązania mają atrybut `[Ignore]` — usuń go, gdy uzupełnisz TODO.

1. `ReportsViewModel.OpenReportAsync` (TODO) — komenda przekazuje Id zgłoszenia jako parametr:
   ```csharp
   [RelayCommand]
   private Task OpenReportAsync(ReportItem item) =>
       Navigation.NavigateToAsync<ReportDetailsViewModel>(item.Id);
   ```
2. `ReportDetailsViewModel.OnNavigatedToAsync` (TODO) — odbierz parametr:
   ```csharp
   public override async Task OnNavigatedToAsync(object? parameter)
   {
       var reportId = (Guid)parameter!;
       var result = await reports.GetAsync(reportId);
       await ShowAsync(result);
   }
   ```
3. Tapnięcie karty (TODO w `ReportsPage.xaml`): `TapGestureRecognizer`, `RelativeSource AncestorType`, `CommandParameter="{Binding .}"`.
4. Para w mapie `MauiProgram.CreatePageRoutes()` (TODO) i rejestracje w DI (TODO)
   (`AddViewModels`: `services.AddTransient<ReportDetailsViewModel>()`, `AddPages`: `services.AddTransient<ReportDetailsPage>()`).
5. Sprawdź przyciski akcji: Nowe → „Rozpocznij” → W trakcie → „Zakończ” → Zakończone (brak akcji).
6. **Testy:** usuń `[Ignore]` z `ReportDetailsViewModelTests` i `ReportsViewModelTests.OpenReport_NavigatesToDetails_WithReportId`:
   ```csharp
   _navigation.Verify(n => n.NavigateToAsync<ReportDetailsViewModel>(reportId), Times.Once);
   ```

## Kryteria akceptacji
- Tapnięcie karty otwiera szczegóły: zdjęcie (zastępcze), tytuł, opis, priorytet, status, daty, stan synchronizacji.
- Status zmienia się Nowe → W trakcie → Zakończone; przyciski akcji zależą od dozwolonych przejść, a z Zakończonego
  nie ma akcji („Zgłoszenie zakończone. Brak dostępnych akcji.”).
- Zmiana statusu oznacza zgłoszenie jako „Oczekuje na wysłanie”; po powrocie lista pokazuje nowy status.
- Szybkie wielokrotne tapnięcie karty otwiera szczegóły tylko raz.
- Strzałka „wstecz” w nagłówku szczegółów (`CanNavigateBack="True"`) wraca do listy (iOS: także gest przesunięcia od lewej krawędzi).
- Przycisk wstecz Androida wraca do listy; na liście zamyka aplikację.
- Wysłane zgłoszenie po zmianie statusu („Oczekuje na wysłanie”) nadal nie ma akcji „Usuń” — reguła `Report.CanDelete`
  (`IsOnServer`: czy zgłoszenie choć raz trafiło do API) jest w modelu, a `ReportItem.CanDelete` na karcie.
- `dotnet test` kończy się na zielono.

## Podpowiedzi
- ViewModel nie zna stron — podaje tylko typ docelowego ViewModelu i parametr. Stronę dobiera `NavigationService` z mapy `PageRoutes`.
- Brak pary w mapie = wyjątek `KeyNotFoundException` przy pierwszej próbie nawigacji (od razu widać, czego zapomniałeś).
- Reguła przejść jest w `Report`, nie w ViewModelu ani XAML. Nowy status = zmiana w modelu i zasobach, bez zmian w ViewModelu i XAML (akcje z `BindableLayout`).
- Shell (`GoToAsync("details?id=1")`, `IQueryAttributable`) to dobry wybór dla prostych aplikacji z zakładkami; my potrzebujemy pełnej kontroli nad stosem (w bloku 08: okna modalne i reset stosu).

## Jeśli utkniesz
```bash
# odłóż swoje zmiany (git stash), pobierz nowe gałęzie (git fetch) i przejdź do gotowego rozwiązania
git stash && git fetch && git checkout 07-navigation
```
