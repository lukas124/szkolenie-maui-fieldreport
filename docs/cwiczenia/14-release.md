# Ćwiczenie 14 — Konfiguracja Test obok Debug

**Blok:** 14 · Wdrożenie: środowiska i publikacja · dzień 2 · **Start:** gałąź `14-release-start` · **Czas:** 7 min (pokaz + samodzielnie)

## Cel
DEV, TEST i PROD obok siebie na jednym telefonie — każde z własnym `ApplicationId`, nazwą, ikoną, konfiguracją API
i aplikacją w Firebase. Ustawienia pokazują środowisko i wersję.

## Co jest w checkpoincie (rozwiązanie)
| Element | Gdzie | Debug | Test | Release |
|---|---|---|---|---|
| `ApplicationId` | csproj | `pl.szkolenie.fieldreport.dev` | `pl.szkolenie.fieldreport.test` | `pl.szkolenie.fieldreport` |
| `ApplicationTitle` | csproj | FieldReport DEV | FieldReport TEST | FieldReport |
| ikona | `MauiIcon` z warunkiem | pomarańczowa | niebieska z paskiem „TEST” (`Resources/AppIcon/Test/`) | pomarańczowa |
| `appsettings.{Configuration}.json` | zasób osadzony `appsettings.environment.json` | `Environment:Name` = DEV | TEST | PROD |
| Firebase | `Platforms/*/Firebase/$(Configuration)/` | aplikacja `.dev` | aplikacja `.test` | aplikacja produkcyjna |
| HTTP bez TLS (Android) | `AndroidManifestPlaceholders` → `${usesCleartextTraffic}` | `true` | `false` | `false` |
| iOS `Info.plist` | `PartialAppManifest` | `Info.Debug.plist` (ATS: localhost) | — | — |
| iOS entitlements | `CodeSignEntitlements` | — (bez push) | — | `Entitlements.Release.plist` (`aps-environment`, tylko build na urządzenie) |
| optymalizacja | csproj | — | `Optimize=true`, `DebugSymbols=false` (release-like) | domyślna Release |
| „Kopiuj token FCM” | `#if DEBUG` | tak | nie | nie |

- `MauiProgram.AddAppConfiguration()` — `appsettings.json` + plik bieżącej konfiguracji → `ApiSettings`, `AppEnvironmentSettings`.
- Wersja: `ApplicationDisplayVersion` `1.0.0` (dla ludzi) + `ApplicationVersion` `42` (zawsze rośnie) → Ustawienia „1.0.0 (42)”.
- `FieldReport.slnx` ma konfigurację `Test` (`<BuildType Name="Test" />`).

## Kroki
Przejdź na gałąź startową ćwiczenia. Trener udostępnia gałęzie stopniowo, więc najpierw `git fetch` pobiera te,
które właśnie się pojawiły (masz lokalne zmiany i Git nie pozwala zmienić gałęzi? Najpierw `git stash`):

```bash
# pobierz nowe gałęzie i przejdź na start ćwiczenia
git fetch && git checkout 14-release-start
```

W gałęzi startowej miejsca do uzupełnienia są oznaczone `TODO:` (lista: Task List w Visual Studio, TODO w Riderze
albo `git grep -n "TODO:"`).
W gałęzi startowej są już: Debug i Release z własnym `ApplicationId`, `appsettings.json`, `appsettings.Debug.json`,
`appsettings.Release.json`, pliki ikony TEST w `Resources/AppIcon/Test/` i pipeline'y CI/CD.

1. (TODO w csproj) Dodaj `<Configurations>Debug;Test;Release</Configurations>` i `PropertyGroup` dla `Test`
   (`ApplicationId`, `ApplicationTitle`, `Optimize`, `DebugSymbols`).
2. (TODO) Utwórz `appsettings.Test.json` (`{"Environment":{"Name":"TEST"}}`) i sprawdź `EmbeddedResource` z warunkiem `Exists(...)`.
3. (TODO w csproj) Ikona TEST: drugi `MauiIcon` z `Condition="'$(Configuration)' == 'Test'"`.
4. Zbuduj i zainstaluj TEST obok DEV:
   ```bash
   # build konfiguracji Test (APK release-like, podpisany kluczem debug)
   dotnet build src/FieldReport.App -f net10.0-android -c Test
   # instalacja TEST obok DEV (-r = zastąp, jeśli już jest)
   adb install -r src/FieldReport.App/bin/Test/net10.0-android/pl.szkolenie.fieldreport.test-Signed.apk
   # DEV (Debug): build, instalacja i uruchomienie
   dotnet build src/FieldReport.App -t:Run -f net10.0-android
   ```
5. Zaloguj się w obu (np. `jan.dev@firma.pl` i `jan.test@firma.pl`) i porównaj Ustawienia.

## Kryteria akceptacji
- Na ekranie emulatora są dwie ikony: „FieldReport DEV” (pomarańczowa) i „FieldReport TEST” (niebieska, z paskiem „TEST”).
- Ustawienia w TEST: technik, środowisko **TEST**, wersja **1.0.0 (42)**, stan powiadomień, „Wyloguj”; bez „Kopiuj token FCM”.
- Push działa w obu aplikacjach (osobne tokeny: każda konfiguracja ma własną aplikację w projekcie Firebase).
- Build TEST jest release-like (optymalizacja, przycinanie) i działa tak samo jak DEV — zawsze testujemy build Release!
- W APK TEST `android:usesCleartextTraffic="false"` (sprawdź: `aapt2 dump xmltree <apk> --file AndroidManifest.xml`).
- `dotnet test` na zielono.
- Build Test/Release bez Playground (narzędzie deweloperskie tylko w Debug, jak „Kopiuj token FCM”).
- Przyrostowy build Test po zmianie zasobów też startuje (`AndroidEnableMarshalMethods=false`; po zmianie tej właściwości raz wyczyść `obj/Test`).

## Publikacja (omówienie)
- **Android:** keystore (tylko w magazynie CI), AAB: `dotnet publish src/FieldReport.App -f net10.0-android -c Release`,
  ścieżki Google Play: internal → closed → production.
- **iOS:** certyfikat + provisioning profile z Push Notifications, `dotnet publish -f net10.0-ios -c Release`, TestFlight.
  Bez płatnego konta build na urządzenie z `aps-environment` się nie podpisze (dlatego entitlements tylko w Release na `ios-*`).
- Sekrety: w repo tylko placeholdery; `.gitignore`: `*.jks`, `*.keystore`, `*.p12`, `*.p8`, `*.mobileprovision`, klucze kont serwisowych.
- Produkcja w osobnym projekcie Firebase (na szkoleniu wszystkie trzy aplikacje są w `fieldreport-training`).
- CI/CD w repozytorium (szczegóły w README, sekcja „CI/CD”):
  - `.github/workflows/ci.yml` / `azure-pipelines/ci.yml` — build Androida i `dotnet test` przy każdym pushu i PR,
  - `.github/workflows/cd.yml` / `azure-pipelines/cd.yml` — ręczne wydanie: GitVersion → wersja → podpisany AAB i IPA
    (sekrety tylko w magazynie CI, w YAML są ich nazwy),
  - `GitVersion.yml` — wersja z tagów gita: `ApplicationDisplayVersion` = SemVer (np. `1.0.1`),
    `ApplicationVersion` = numer uruchomienia pipeline'u (zawsze rośnie).

## Jeśli utkniesz
```bash
# odłóż swoje zmiany (git stash), pobierz nowe gałęzie (git fetch) i przejdź do gotowego rozwiązania
git stash && git fetch && git checkout 14-release
```
