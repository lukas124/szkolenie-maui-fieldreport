# Ćwiczenie 06 — Compiled bindings i konwerter priorytetu

**Blok:** 06 · Binding (wiązanie danych) · dzień 1 · **Start:** gałąź `06-binding-start` · **Czas:** 5 min

## Cel
Bindingi sprawdzane w czasie kompilacji, wygląd zależny od znaczenia przez konwertery, test formattera dla pl-PL.

## Co jest w gałęzi startowej
- `x:DataType` na stronach, szablonach i komponentach; w `FieldReport.App.csproj`:
  ```xml
  <MauiEnableXamlCBindingWithSourceCompilation>true</MauiEnableXamlCBindingWithSourceCompilation>
  <MauiXamlInflator>SourceGen</MauiXamlInflator>
  <WarningsAsErrors>$(WarningsAsErrors);MAUIG2045</WarningsAsErrors>
  ```
- Konwertery w `App/Converters` zarejestrowane w `App.xaml` (kolory jako właściwości):
  ```xml
  <conv:PriorityToColorConverter x:Key="PriorityToColor"
      High="{StaticResource DangerColor}" Normal="{StaticResource WarningColor}" Low="{StaticResource NeutralColor}" />
  ```
  i użyte w odznakach: `Fill="{Binding Priority, Source={x:Reference Root}, Converter={StaticResource PriorityToColor}}"`
  (zamiast `DataTrigger` z 03). `StatusToColorConverter` jest gotowy, `PriorityToColorConverter` — do napisania.

## Kroki
Przejdź na gałąź startową ćwiczenia. Trener udostępnia gałęzie stopniowo, więc najpierw `git fetch` pobiera te,
które właśnie się pojawiły (masz lokalne zmiany i Git nie pozwala zmienić gałęzi? Najpierw `git stash`):

```bash
# pobierz nowe gałęzie i przejdź na start ćwiczenia
git fetch && git checkout 06-binding-start
```

W gałęzi startowej miejsca do uzupełnienia są oznaczone `TODO:` (lista: Task List w Visual Studio, TODO w Riderze
albo `git grep -n "TODO:"`). Testy rozwiązania mają atrybut `[Ignore]` — usuń go, gdy uzupełnisz TODO.

1. Zrób celową literówkę w bindingu (np. `{Binding Titel}` w szablonie listy) → **błąd kompilacji** zamiast cichego braku danych. Cofnij ją.
2. `PriorityToColorConverter.Convert` (TODO): `High` → `High`, `Normal` → `Normal`, pozostałe → `Low` (wzór: `StatusToColorConverter`).
3. **Test** `ReportFormatterTests.FormatDate_UsesPolishShortDateAndTime` (TODO): data w kulturze pl-PL (`26.09.2026 14:05`);
   statusy i priorytety z kluczy zasobów są już przetestowane (`KeyLocalizer` zwraca sam klucz).

## Kryteria akceptacji
- Build bez ostrzeżeń bindingów; literówka w ścieżce bindingu przerywa build.
- Kolory kropek priorytetu i statusu są takie jak w bloku 03, ale teraz pochodzą z konwerterów.
- `dotnet test` kończy się na zielono (w tym test formattera pl-PL).

## Podpowiedzi
- Konwertery ogólnego przeznaczenia (`InvertedBoolConverter`, `IsNotNullConverter`, `IsStringNotNullOrEmptyConverter`…) są gotowe w **CommunityToolkit.Maui** — dołączymy je w bloku 07. Własny konwerter piszemy tylko wtedy, gdy zawiera logikę naszej domeny (priorytet/status → kolor).
- Kolor **nigdy** nie jest właściwością ViewModelu (w `Core` bez MAUI i tak się nie skompiluje).
- Tryby: `OneWay` (domyślny dla większości), `TwoWay` (domyślny dla `Entry.Text`), `OneTime`.
- `StringFormat` vs gotowy tekst z formattera: tekst dla użytkownika z `IReportFormatter` (testowalny, kultura pod kontrolą).
- Binding z `Source={x:Reference …}` na stronie z innym `x:DataType` → ustaw `x:DataType` na elemencie (np. `x:DataType="Entry"` w Playground).
- Test daty niezależny od strefy maszyny: zbuduj `DateTimeOffset` z lokalnym przesunięciem (`TimeZoneInfo.Local.GetUtcOffset`).

## Jeśli utkniesz
```bash
# odłóż swoje zmiany (git stash), pobierz nowe gałęzie (git fetch) i przejdź do gotowego rozwiązania
git stash && git fetch && git checkout 06-binding
```
