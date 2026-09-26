# Ćwiczenie 01 — Rozruch 01-start

**Blok:** 01 · Rozruch: projekt MAUI i XAML · dzień 1 · **Start:** gałąź `01-start` · **Czas:** 10 min

## Cel
Masz działający projekt, rozumiesz pliki projektu MAUI i widzisz cykl życia okna w logach.

## Kroki
1. Pobierz repozytorium, przejdź na checkpoint i otwórz rozwiązanie `FieldReport.slnx` w IDE (albo uruchom z terminala):
   ```bash
   # przejście na checkpoint startowy
   git checkout 01-start
   # build, instalacja i uruchomienie na emulatorze/telefonie
   dotnet build src/FieldReport.App -t:Run -f net10.0-android
   ```
2. W `src/FieldReport.App/FieldReport.App.csproj` zmień `ApplicationTitle` (np. „FieldReport Anna”) i uruchom ponownie. Sprawdź nazwę pod ikoną.
3. Zmień ikonę i kolor splash:
   - `Resources/AppIcon/appiconfg.svg` (rysunek na ikonie; `fg` = ang. foreground) lub atrybut `Color` w `<MauiIcon>`,
   - atrybut `Color` w `<MauiSplashScreen>`.
4. W `App.xaml.cs` (`CreateWindow`) obejrzyj logowanie zdarzeń okna (`Created`, `Activated`, `Deactivated`, `Stopped`, `Resumed`, `Destroying`). Dopisz własny komunikat, np. do `Resumed`.
5. Zminimalizuj aplikację i wróć do niej. Obejrzyj logi:
   ```bash
   # logi aplikacji .NET z urządzenia (tylko wpisy z tagiem DOTNET)
   adb logcat -s DOTNET
   ```
6. (dla chętnych) Opcje programisty Androida → „Nie zachowuj aktywności”. Zminimalizuj i wróć: okno jest tworzone od nowa (`Created`).
7. Otwórz Playground (zębatka w nagłówku → Ustawienia → Playground; strzałka „wstecz” w nagłówku wraca), wpisz imię i obejrzyj binding `x:Reference`. Zmień `StringFormat`.
8. Uruchom testy:
   ```bash
   # wszystkie testy jednostkowe (bez emulatora)
   dotnet test
   ```

## Kryteria akceptacji
- Na ekranie głównym emulatora/telefonu widać nową nazwę i ikonę, a przy starcie Twój kolor splash.
- W `adb logcat -s DOTNET` po minimalizacji i powrocie widać kolejno: `Window: Deactivated` → `Stopped` → `Resumed` → `Activated`.
- Playground: wpisanie imienia zmienia tekst „Cześć …!”.
- Playground: przy pustym polu „Imię” licznik (etykieta z `x:Reference`) pokazuje „Liczba znaków: 0” (`Text` pustego `Entry` to `null` — ścieżka `Text.Length` nie daje wartości, więc potrzebny `FallbackValue`/`TargetNullValue`).
- `dotnet test` — wszystkie testy na zielono.

## Podpowiedzi
- Ikona „nie chce się zmienić”: `dotnet clean`, odinstaluj aplikację z emulatora i uruchom ponownie (cache launchera).
- Pierwszy build i deploy trwają długo — to normalne.
- Emulator bez akceleracji → telefon po USB.
- Hot Reload złapie zmianę XAML i ciała metod, ale nie konstruktora, rejestracji w DI ani nowych plików.
- Routing startowy zawsze w zdarzeniach okna (`Window.Created`), a nie w `OnStart`: Android odtwarza okno przy żywym procesie.
- `MainActivity` ma rozszerzone `ConfigurationChanges` (m.in. `UiMode`, `FontScale`, `Locale`, `Density`): zmiana motywu, rozmiaru czcionki czy języka nie odtwarza aktywności, więc okno MAUI (i stos nawigacji) zostaje.

## Jeśli utkniesz
```bash
# odłóż swoje zmiany (git stash) i przejdź do gotowego rozwiązania tego bloku
git stash && git checkout 01-start
```
