# FieldReport — aplikacja szkoleniowa „Podstawy .NET MAUI”

Repozytorium z kodem do 2-dniowego szkolenia **Podstawy budowy aplikacji wieloplatformowych w .NET MAUI**.
Budujemy krok po kroku aplikację **FieldReport**: technik serwisowy w terenie przyjmuje zgłoszenia od dyspozytora,
zakłada własne (ze zdjęciem usterki), prowadzi je do zakończenia i pracuje również bez zasięgu.

## Funkcje

| Funkcja | Opis |
|---|---|
| Start i routing | Splash, sprawdzenie sesji, logowanie albo lista; deep link po starcie |
| Logowanie technika | E-mail + PIN, token w `SecureStorage`, odświeżanie tokenu, wylogowanie |
| Lista zgłoszeń | Karty (miniatura, tytuł, data, priorytet, status, „Oczekuje na wysłanie”), pull-to-refresh, pusty stan, usuwanie przesunięciem z potwierdzeniem |
| Szczegóły zgłoszenia | Zdjęcie, opis, daty, zmiana statusu Nowe → W trakcie → Zakończone (reguła w modelu `Report`) |
| Kreator nowego zgłoszenia | 3 kroki: dane → zdjęcie (aparat/galeria) → podsumowanie |
| Offline i synchronizacja | SQLite tylko dla niewysłanych, wysyłka i usunięcie z telefonu, `POST` z nagłówkiem `Idempotency-Key` bez duplikatów |
| Powiadomienia push | Firebase Cloud Messaging, tapnięcie → szczegóły |
| Ustawienia | Technik, środowisko, wersja, powiadomienia, wylogowanie |

## Struktura rozwiązania

```
FieldReport.slnx
├── src/
│   ├── FieldReport.Core/     # net10.0, BEZ MAUI: Models, ValueObjects, Services, Data, ViewModels, Presentation, Navigation
│   └── FieldReport.App/      # MAUI: strony, komponenty, konwertery, MauiProgram, Platforms/
├── tests/FieldReport.Tests/  # NUnit + Moq, net10.0 (bez emulatora)
└── docs/cwiczenia/           # treść ćwiczeń do każdego bloku
```

Jedna zasada architektoniczna: **`FieldReport.Core` nie referuje MAUI**. ViewModele testujemy w sekundę, bez emulatora.

## Wymagania

- .NET 10 SDK (10.0.100+) i workload MAUI: `dotnet workload install maui-android` (na Macu także `maui-ios`)
- Android SDK + emulator (API 35 lub 36) **z obrazem Google Play** (potrzebny do push w dniu 2) albo telefon z włączonym debugowaniem USB
- opcjonalnie Mac z Xcode (iOS)
- IDE: Visual Studio 2026, JetBrains Rider albo VS Code z C# Dev Kit + .NET MAUI
- Git

## Jak uruchomić

```bash
# testy jednostkowe (bez emulatora)
dotnet test

# Android: build, instalacja i uruchomienie (-t:Run) na emulatorze lub podłączonym telefonie
dotnet build src/FieldReport.App -t:Run -f net10.0-android

# iOS: to samo na symulatorze (tylko macOS; tam target net10.0-ios jest dodawany automatycznie)
dotnet build src/FieldReport.App -t:Run -f net10.0-ios
# (csproj ma ValidateXcodeVersion=false: build nie blokuje się, gdy .NET for iOS oczekuje nieco nowszego Xcode)
```

Logi aplikacji (m.in. zdarzenia cyklu życia okna) na Androidzie: `adb logcat -s DOTNET`.

## API szkoleniowe

Aplikacja od checkpointu `11-push` rozmawia z **API szkoleniowym** (ASP.NET Core, Swagger, Firebase Admin SDK),
które jest w **osobnym repozytorium** `lukas124/fieldreport-training-api`.

| Co | Adres |
|---|---|
| Baza API (aplikacja) | `https://mobilny.dev/maui/podstawowe/` |
| Swagger (dyspozytor, próby na żywo) | https://mobilny.dev/maui/podstawowe/swagger |
| Lokalnie (plan B, API uruchomione na laptopie) | `http://localhost:5080/`, z emulatora Androida `http://10.0.2.2:5080/` |

- Logowanie: **dowolny poprawny e-mail + PIN `1234`**; każdy e-mail to osobny technik z osobnymi danymi.
- Dyspozytor (bez logowania, w Swaggerze): `POST /api/dispatcher/reports` z body
  `{"technicianEmail":"jan.kowalski@firma.pl","title":"Awaria pompy","description":"…","priority":"High"}`
  tworzy zgłoszenie technika i wysyła push `report_assigned`; `PATCH /api/dispatcher/reports/{id}/status` z `{"status":"InProgress"}` —
  push `report_status_changed`.
- Adres bazowy **kończy się `/`**, a ścieżki w klientach są względne (`api/reports`, bez `/` na początku) — inaczej
  `HttpClient` zgubi prefiks `/maui/podstawowe`.
- JSON: camelCase, enumy jako tekst (`"priority":"High"`, `"status":"InProgress"`).

## Checkpointy

Każdy blok ćwiczeń ma dwie gałęzie:

- **`NN-nazwa-start`** — punkt startowy ćwiczenia: stan poprzedniego checkpointu + elementy przygotowane w szablonie
  (modele, interfejsy, strony, zasoby). Miejsca do uzupełnienia są oznaczone komentarzem `TODO:` (krótko: co zrobić),
  a testy rozwiązania mają atrybut `[Ignore("Ćwiczenie NN: usuń Ignore po uzupełnieniu TODO")]`. Gałąź startowa
  zawsze się buduje; po uzupełnieniu TODO i usunięciu `[Ignore]` testy przechodzą.
- **`NN-nazwa`** (gałąź i tag) — **rozwiązanie ćwiczenia** z danego bloku; jednocześnie stan, od którego zaczyna się następny blok.

Blok 01 nie ma gałęzi startowej — zaczyna się od `01-start`. Numer bloku = numer checkpointu.
Treść ćwiczeń: [`docs/cwiczenia/`](docs/cwiczenia/).

| Blok | Start | Rozwiązanie | Temat | Co zawiera rozwiązanie |
|---|---|---|---|---|
| 01 | `01-start` | `01-start` | Rozruch: projekt MAUI i XAML | szkielet Core/App/Tests, `MauiProgram`, cykl życia okna, Playground |
| 02 | `02-mvvm-start` | `02-mvvm` | MVVM i ViewModel zgodny z SRP | `BaseViewModel`, `BasePage`, `ReportItem` + fabryka + formatter, `ReportsViewModel`, lokalizacja: `Strings.resx` + `{loc:Translate}` + `ILocalizer`; obiekty listy składane ręcznie w `App.CreateWindow` |
| 03 | `03-ui-start` | `03-ui` | Layouty, kontrolki, style | karta zgłoszenia, `StatusBadge`, `PriorityBadge`, `EmptyView`, tryb ciemny |
| 04 | `04-di-start` | `04-di` | Wstrzykiwanie zależności (ang. Dependency Injection) | kontener DI zamiast ręcznego składania w `App` (przed/po), `AddCoreServices/AddPlatformServices/AddViewModels/AddPages`, `InMemoryReportRepository` za `IReportService` |
| 05 | `05-commands-start` | `05-commands` | Komendy, async/await i wątek UI | usuwanie przez `SwipeView` z potwierdzeniem, `IUserFeedback`, `IErrorMessageProvider`, `Result` |
| 06 | `06-binding-start` | `06-binding` | Binding (wiązanie danych) | compiled bindings (`x:DataType`), konwertery priorytetu i statusu, test formattera pl-PL |
| 07 | `07-navigation-start` | `07-navigation` | Shell i nawigacja ViewModel-first | `INavigationService` + `PageRoutes` (mapa ViewModel → strona), `RootPage`, szczegóły zgłoszenia, zmiana statusu |
| 08 | `08-flows-start` | `08-flows` | Nawigacja v2, start, logowanie, kreator | `OpenModalAsync` i `ResetToAsync`, `StartupPage`/`StartupViewModel` (start aplikacji), logowanie „na niby” (`FakeAuthService`, PIN `1234`), kreator nowego zgłoszenia na jednej stronie z behaviorem walidacji tytułu, „+” na liście, Ustawienia z przełącznikiem języka PL/EN |
| 09 | `09-camera-start` | `09-camera` | API urządzenia, uprawnienia, aparat | `IMediaService`/`MauiMediaService`, `IPhotoStorage` (skalowanie), `IPermissionService` (odmowa na stałe → ustawienia), krok „Zdjęcie”, wpisy w manifestach |
| 10 | `10-sqlite-start` | `10-sqlite` | SQLite i offline-first | `SqliteReportRepository` (sqlite-net-pcl), `ReportRow` + `ReportRowMapper`, `IAppPaths`, migracje `PRAGMA user_version`; podmiana tylko w DI |
| 11 | `11-push-start` | `11-push` | Push z Firebase Cloud Messaging | `Plugin.Firebase.CloudMessaging` 4.0.1, pliki Firebase per konfiguracja, kanał `reports`, `FirebasePushAdapter`, `PushMessageRouter`, `DeepLinkRouter`, rejestracja urządzenia w API, modal „Włącz powiadomienia”, baner na pierwszym planie, logowanie przez API |
| 12 | `12-api-start` | `12-api` | REST API i synchronizacja | `ReportsApiClient`, `HttpResultExtensions` (+ wyjątki Polly), `AddStandardResilienceHandler`, lista z API + niewysłane z telefonu (offline: tylko niewysłane), `SyncService` (wysyłka i usunięcie z telefonu), zdjęcia z podpisanego `photoUrl`, `RefreshCommand`, wylogowanie z ostrzeżeniem o niewysłanych |
| 13 | `13-tests-start` | `13-tests` | Testy i debugowanie | testy kreatora i synchronizacji offline, testy architektury (Core bez MAUI, ≤ 6 zależności ViewModelu), kompletność zasobów tekstowych (pl i en) |
| 14 | `14-release-start` | `14-release` | Wdrożenie: środowiska i publikacja | konfiguracje Debug/Test/Release (DEV/TEST/PROD obok siebie), `appsettings.{Configuration}.json`, ikona per środowisko, `AndroidManifestPlaceholders`, `PartialAppManifest`, Firebase per konfiguracja, CI/CD (GitHub Actions, Azure DevOps) z GitVersion |

### Praca z checkpointami

Repozytorium rośnie w trakcie szkolenia: na starcie jest w nim tylko `01-start`, a kolejne gałęzie (start ćwiczenia,
potem rozwiązanie) pojawiają się, gdy dojdziemy do danego bloku. Dlatego przed przejściem na nową gałąź zrób `git fetch`.

```bash
git fetch --all --tags         # pobierz nowe gałęzie i tagi (te, które już są udostępnione)
git checkout 02-mvvm-start     # start ćwiczenia z bloku 02
git grep -n "TODO:"            # miejsca do uzupełnienia

# utknąłeś? odłóż swoje zmiany (git stash), pobierz nowe gałęzie i przejdź do rozwiązania bloku:
git stash && git fetch && git checkout 02-mvvm
```

## Licencje

Fonty IBM Plex Sans i JetBrains Mono: SIL Open Font License 1.1 (`docs/licenses/`).
