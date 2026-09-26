# Ćwiczenie 08 — Krok „Dane” kreatora z walidacją

**Blok:** 08 · Nawigacja v2, start, logowanie, kreator · dzień 2 · **Start:** gałąź `08-flows-start` · **Czas:** 5 min (+ dokończenie w przerwie)

## Cel
Pełna nawigacja aplikacji: splash → logowanie/lista (reset stosu), kreator nowego zgłoszenia jako modal z krokami
na jednej stronie. Walidacja tytułu mieszka w modelu (`ReportTitle`), a ViewModel kroku tylko ją wywołuje.

## Co jest w gałęzi startowej
- `INavigationService` rozszerzony o `OpenModalAsync<TViewModel>(object? parameter = null)` (okno modalne)
  i `ResetToAsync<TViewModel>()` (nowy stos, np. lista po zalogowaniu); każda metoda chroniona polem `_isNavigating`
- Start i routing: `RootPage` z `StartupPage` (splash), `StartupViewModel` (sesja → lista albo logowanie, potem
  `IDeepLinkRouter.OnStartupCompletedAsync`), `StartupErrorViewModel` („Spróbuj ponownie”), `App.CreateWindow` z try/catch
- Logowanie technika „na niby”: `LoginViewModel` (`SignInCommand` z `CanExecute` → `ICredentialsValidator`), `IAuthService` →
  `FakeAuthService` (dowolny e-mail, PIN `1234`), sesja w `SecureStorage` (`ISessionStore` → `SecureSessionStore`)
- Kreator nowego zgłoszenia: `NewReportDraft`, `StepSequence`, `WizardStepViewModel`, `DetailsStepViewModel`, `PhotoStepViewModel` (na razie bez aparatu),
  `SummaryStepViewModel`, `NewReportFlowViewModel`, `NewReportStepSelector`, `NewReportFlowPage` (modal),
  `IReportService.CreateAsync(draft)` → `Result<Guid>`
- Behavior `App/Behaviors/TextLengthValidationBehavior` (czerwona ramka poza zakresem 3–80 znaków — tylko wygląd,
  regułę sprawdza `ReportTitle.Create`); podpięcie do pola „Tytuł” jest do zrobienia
- Lista zgłoszeń i Ustawienia: przycisk „+” na liście (ikona SVG, wyśrodkowana), ikona Ustawień w nagłówku, `SettingsPage`
  (technik, środowisko, wersja, **język PL/EN** przełączany w locie przez `ILocalizer.SetLocale`, Playground, „Wyloguj”)
- Nagłówek: „X” (`ShowCloseButton` w `BasePage` → `BaseViewModel.OnCloseRequestedAsync`), strony bez nagłówka
  (`IsHeaderVisible="False"`: logowanie, błąd startu)

## Kroki
Przejdź na gałąź startową ćwiczenia. Trener udostępnia gałęzie stopniowo, więc najpierw `git fetch` pobiera te,
które właśnie się pojawiły (masz lokalne zmiany i Git nie pozwala zmienić gałęzi? Najpierw `git stash`):

```bash
# pobierz nowe gałęzie i przejdź na start ćwiczenia
git fetch && git checkout 08-flows-start
```

W gałęzi startowej miejsca do uzupełnienia są oznaczone `TODO:` (lista: Task List w Visual Studio, TODO w Riderze
albo `git grep -n "TODO:"`). Testy rozwiązania mają atrybut `[Ignore]` — usuń go, gdy uzupełnisz TODO.

1. `DetailsStepViewModel` (TODO): dodaj parametr konstruktora `IErrorMessageProvider messages`
   (`DetailsStepViewModel(IErrorMessageProvider messages, ILocalizer localizer)`; w testach `SetUp` przekaż mock — TODO),
   a potem reakcję na zmianę tytułu:
   ```csharp
   partial void OnTitleInputChanged(string value)
   {
       var title = ReportTitle.Create(value);
       _draft.Title = title.IsSuccess ? title.Value : null;
       IsValid = title.IsSuccess;
       TitleError = title.IsSuccess || value.Length == 0 ? null : messages.GetMessage(title.Error!);
   }
   ```
2. Sprawdź w `NewReportFlowViewModel`, że `NextCommand` ma `CanExecute = nameof(CanGoNext)` i że zmiana `IsValid`
   kroku wywołuje `NextCommand.NotifyCanExecuteChanged()` (zdarzenie `ValidityChanged`).
3. Behavior walidacji na polu „Tytuł” (TODO w `NewReportFlowPage.xaml`; granice z value objectu,
   ramka przez `x:Reference` — `Border` ma `x:Name="TitleFrame"`):
   ```xml
   <Entry.Behaviors>
       <behaviors:TextLengthValidationBehavior MinLength="{x:Static vo:ReportTitle.MinLength}"
                                               MaxLength="{x:Static vo:ReportTitle.MaxLength}"
                                               TargetBorder="{x:Reference TitleFrame}"
                                               InvalidStrokeColor="{StaticResource DangerColor}" />
   </Entry.Behaviors>
   ```
4. **Testy:** usuń atrybut `[Ignore]` z testów w klasach `DetailsStepViewModelTests` i `NewReportFlowViewModelTests`
   (bez niego NUnit znowu je uruchamia — nie komentuj testów, tylko usuń atrybut):
   - `ReportTitle.Create`: 2 znaki → błąd, 3 → OK, 80 → OK, 81 → błąd (`[TestCase]`, gotowe w `ReportTitleTests`)
   - „Dalej” nieaktywne przy tytule `ab`:
     ```csharp
     _details.TitleInput = "ab";
     Assert.That(_vm.NextCommand.CanExecute(null), Is.False);
     ```

## Kryteria akceptacji
- **Start i routing:** splash widać od razu; bez sesji → logowanie, z sesją (po restarcie aplikacji) → lista. Wstecz z listy
  zamyka aplikację (strona startowa i logowanie nie zostają na stosie). Wyjątek podczas startu → ekran
  „Nie udało się uruchomić aplikacji” z „Spróbuj ponownie” (bez crasha).
- **Logowanie technika:** „Zaloguj” aktywne tylko dla poprawnego e-maila i PIN-u 4–6 cyfr; zły PIN → „Nieprawidłowy e-mail lub PIN.”;
  po sukcesie lista (wstecz nie wraca do logowania).
- **Kreator nowego zgłoszenia:** „+” otwiera kreator (modal) z nagłówkiem „Nowe zgłoszenie · 1 / 3” i paskiem postępu; tytuł `ab` → komunikat pod polem i nieaktywne
  „Dalej”, a ramka pola tytułu jest czerwona (behavior); poprawny tytuł → „Dalej” przechodzi do kroku „Zdjęcie”; priorytet domyślnie Normalny; systemowy „wstecz” na kroku 2/3 → poprzedni krok, na kroku 1 →
  „Porzucić zgłoszenie?” (Nie = zostaje, Tak = zamyka); „X” w nagłówku pyta o porzucenie na każdym kroku. „Zapisz” → lista z nowym zgłoszeniem („Nowe”,
  „Oczekuje na wysłanie”) na górze.
- **Ustawienia:** ikona w nagłówku → Ustawienia (technik z e-maila, środowisko DEV, wersja `1.0.0 (42)`); „Wyloguj” z
  potwierdzeniem → logowanie. Zmiana języka na „English” od razu zmienia teksty ekranu; wybór zostaje po restarcie.
- Szybkie wielokrotne tapnięcie „+” lub ikony Ustawień otwiera stronę tylko raz; podwójne tapnięcie „Zapisz”/„Zaloguj”
  wykonuje akcję raz (komenda `[RelayCommand]` async = `AsyncRelayCommand`, który nie startuje drugi raz, dopóki trwa pierwsze wykonanie).
- Testy, z których usunięto `[Ignore]`, przechodzą (`dotnet test` na zielono).

## Podpowiedzi
- Blokada nawigacji (`_isNavigating = true`) jest zakładana **przed** tworzeniem strony i zwalniana w `finally` **po zmianie stosu**,
  a nie po załadowaniu danych — dzięki temu `OnNavigatedToAsync` może sam nawigować (np. `StartupViewModel`).
- Behavior to klasa `Behavior<T>` z metodami `OnAttachedTo`/`OnDetachingFrom`: podpina się do zdarzeń kontrolki
  (tu `TextChanged` pola) i zmienia wygląd — tu kolor ramki `Border` wskazanej przez `x:Reference`. Logiki biznesowej w nim nie trzymamy.
- Reset: najpierw `PushAsync` nowej strony, dopiero potem `RemovePage` starych — stos nigdy nie jest pusty.
- MAUI nie ma `ContentControl`: bieżący krok to jednoelementowa kolekcja `CurrentStepHost` + `BindableLayout`
  z `ItemTemplateSelector`.
- `OnNavigatedToAsync` odpala się raz, `OnAppearing` także po zamknięciu modala (lista odświeża się po powrocie z kreatora).
- iOS: kreator ma `ios:Page.ModalPresentationStyle="FullScreen"` — domyślny arkusz (page sheet) dałoby się zamknąć
  gestem w dół z pominięciem pytania „Porzucić zgłoszenie?”.

## Jeśli utkniesz
```bash
# odłóż swoje zmiany (git stash), pobierz nowe gałęzie (git fetch) i przejdź do gotowego rozwiązania
git stash && git fetch && git checkout 08-flows
```
