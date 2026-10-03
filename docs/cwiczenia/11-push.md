# Ćwiczenie 11 — Powiadomienia push od początku do końca

**Blok:** 11 · Push z Firebase Cloud Messaging · dzień 2 · **Start:** gałąź `11-push-start` · **Czas:** 13 min

## Cel
Push end-to-end: dyspozytor (API szkoleniowe / Swagger) → Firebase → urządzenie → tapnięcie → szczegóły zgłoszenia.
Zrozumienie, co dzieje się w każdym stanie aplikacji: pierwszy plan, tło, aplikacja zamknięta.

## Co jest w gałęzi startowej
- **Plugin** `Plugin.Firebase.CloudMessaging` 4.0.1 (+ `Plugin.Firebase.Core`) — wersja dla .NET 9 działa z .NET 10 / MAUI 10.
  Inicjalizacja w `MauiProgram.RegisterFirebaseServices()` (zdarzenia cyklu życia Android/iOS).
- **Pliki Firebase** (konfiguracja klienta, nie sekret): `Platforms/Android/Firebase/{Debug,Test,Release}/google-services.json`,
  `Platforms/iOS/Firebase/{Debug,Test,Release}/GoogleService-Info.plist` — do buildu trafia plik bieżącej konfiguracji (csproj).
- **Android**: kanał `reports` (`MainApplication`), `POST_NOTIFICATIONS`, ikona `ic_notification`, `MainActivity.OnNewIntent`
  → plugin (tapnięcie w tle / zimny start), na pierwszym planie bez powiadomienia systemowego (baner w aplikacji).
- **iOS**: bez capability Push i bez `aps-environment` (brak płatnego konta) — token FCM nie powstaje, rejestracja jest
  pomijana, aplikacja działa. `ForegroundNotificationDelegate` (pierwszy plan: baner w aplikacji zamiast systemowego).
  `tools/push/report_assigned.apns` i `report_status_changed.apns` do `xcrun simctl push`.
- **Core**: `PushMessageRouter` (payload → `DeepLink`), `DeepLinkRouter` (z 08), `DeviceRegistrationService`,
  `NotificationOnboarding` (ekran zgody tylko po pierwszym logowaniu), `NotificationPrimingViewModel` („Włącz” do napisania),
  `ForegroundPushHandler` (baner + odświeżenie listy), `NotificationSettingsService`, `AccountService` (wylogowanie
  = wyrejestrowanie urządzenia + usunięcie sesji).
- **App**: `FirebasePushAdapter` (jedyna klasa znająca plugin; obsługa tapnięcia do napisania), `MauiPushPermissionService`, `SnackbarInAppNotifier`
  (CommunityToolkit.Maui), `PushStartup`, strona `NotificationPrimingPage`, Ustawienia: stan powiadomień, „Włącz”,
  (Debug) „Kopiuj token FCM”.
- **Logowanie przez API**: rejestracja urządzenia w API wymaga prawdziwego tokenu logowania, więc już tutaj
  są `ApiAuthService` (zamiast `FakeAuthService`: zmiana rejestracji w DI, klasa `FakeAuthService` znika), `AuthApiClient`, `DevicesApiClient`, `HttpResultExtensions`,
  `AuthHeaderHandler` (Bearer, po 401 odświeżenie tokenu i jedno ponowienie). API: `https://mobilny.dev/maui/podstawowe/`.
- Wylogowanie: `DELETE api/devices/{token}` („best effort” — offline też można się wylogować) + `IPushTokenProvider.DeleteTokenAsync()` (token usuwany na telefonie w SDK Firebase — powiadomienia na stary token już nie dotrą); baner push tylko dla zalogowanego.
- `AuthHeaderHandler` w `Core/Data/Api` (testowany bez emulatora): 401 → `IAuthService.RefreshSessionAsync(usedAccessToken)` → jedno ponowienie;
  równoległe 401 odświeżają token raz — pod blokadą odświeża tylko pierwsze żądanie, pozostałe zastają w sesji nowy token (refresh token jest jednorazowy).
- `BaseViewModel.OnResumedAsync()` — `App` woła go dla strony na wierzchu po `Window.Resumed` (Ustawienia odświeżają stan zgody po powrocie z ustawień systemu).
- Rejestracja tokenu także wtedy, gdy zgodę włączono poza aplikacją (ustawienia systemu po „Nie teraz” albo po odmowie): `IDeviceRegistrationService.RefreshAsync()` przy starcie (`PushStartup.Start`) i po `Window.Resumed` (`PushStartup.OnResumed()`); ekran Ustawień tylko odczytuje stan zgody (`INotificationSettingsService.GetStatusAsync()`), a rejestruje wyłącznie `PushStartup` i „Włącz” (`EnableAsync`). API przyjmuje ten sam token wielokrotnie bez duplikatów.
- Wylogowanie bez internetu: usunięcie tokenu FCM wymaga sieci, więc `RefreshAsync()` przy kolejnym starcie / powrocie z tła usuwa token, jeśli nikt nie jest zalogowany — wylogowany telefon przestaje dostawać powiadomienia.
- „Włącz” przy odmowie na stałe (także ustawionej poza aplikacją, gdy systemowa prośba od razu zwraca odmowę): otwierają się
  ustawienia systemu przez `IPermissionService.OpenAppSettings()` — tę samą metodę, której używa krok „Zdjęcie”.
- `PushStartup` dostaje fabrykę `Func<IDeviceRegistrationService>` (jak `SyncScheduler` w bloku REST) i jawnie uruchamia
  nasłuch: `ForegroundPushHandler.Start()`; `App` uruchamia `SessionExpiredHandler.Start()`.
- Kanał powiadomień Androida: nazwa i opis z zasobów (`Push_ChannelName`, `Push_ChannelDescription`, PL/EN). Po zmianie języka
  w aplikacji `MainApplication` tworzy kanał ponownie z tym samym identyfikatorem — nazwa w ustawieniach systemu zmienia się od razu.

## Kroki
Przejdź na gałąź startową ćwiczenia. Trener udostępnia gałęzie stopniowo, więc najpierw `git fetch` pobiera te,
które właśnie się pojawiły (masz lokalne zmiany i Git nie pozwala zmienić gałęzi? Najpierw `git stash`):

```bash
# pobierz nowe gałęzie i przejdź na start ćwiczenia
git fetch && git checkout 11-push-start
```

W gałęzi startowej miejsca do uzupełnienia są oznaczone `TODO:` (lista: Task List w Visual Studio, TODO w Riderze
albo `git grep -n "TODO:"`). Testy rozwiązania mają atrybut `[Ignore]` — usuń go, gdy uzupełnisz TODO.

1. `FirebasePushAdapter` (TODO): dodaj parametry konstruktora `IPushMessageRouter router` i `IDeepLinkRouter deepLinks`,
   a potem obsługę tapnięcia `OnTappedAsync`:
   ```csharp
   private Task OnTappedAsync(IDictionary<string, string>? data) =>
       router.ToDeepLink(ToReadOnly(data)) is { } link ? deepLinks.RouteAsync(link) : Task.CompletedTask;
   ```
   i przeczytaj `DeepLinkRouter` (link z zimnego startu czeka na `OnStartupCompletedAsync`).
2. `NotificationPrimingViewModel` (TODO): parametry `IPushPermissionService permission`, `IDeviceRegistrationService devices`,
   `IUserFeedback feedback` (obok `INavigationService navigation`; w `SetUp` testów przekaż mocki — TODO), a potem `EnableAsync`
   — rejestracja po zgodzie:
   ```csharp
   if (await permission.RequestAsync() == PushPermissionStatus.Granted)
   {
       var result = await devices.RegisterAsync(ct);
       if (!result.IsSuccess) await feedback.ShowErrorAsync(result.Error!);
   }
   await Navigation.GoBackAsync();
   ```
3. **Testy:** usuń `[Ignore]` z testów `Enable_*` w `NotificationPrimingViewModelTests`. Obejrzyj `PushMessageRouterTests`:
   poprawny payload (`report_assigned`, `report_status_changed`), nieznany typ → `null`, zły `reportId` → `null`.
4. Uruchom na emulatorze **z Google Play**, zaloguj się (dowolny e-mail, PIN `1234`), wybierz „Włącz” i „Zezwalaj”
   (ang. „Allow”), a potem podaj trenerowi swój e-mail. Trener wysyła push z API (albo Ty w Swaggerze:
   `POST /api/dispatcher/reports` z `technicianEmail` = Twój e-mail).
5. (Mac) iOS: `xcrun simctl push booted pl.szkolenie.fieldreport.dev tools/push/report_assigned.apns`; potem własny plik
   dla `report_status_changed` z `reportId` zgłoszenia z listy (np. z bazy: `xcrun simctl get_app_container booted pl.szkolenie.fieldreport.dev data`).
6. **Plan B:** Ustawienia → „Kopiuj token FCM” → konsola Firebase → „Wyślij wiadomość testową”.

## Kryteria akceptacji
- Po **pierwszym** logowaniu modal „Włącz powiadomienia” („Włącz” → systemowa prośba Androida 13+, „Nie teraz” → lista);
  przy kolejnych logowaniach i przy zimnym starcie modal się nie pokazuje.
- Po zgodzie token jest zarejestrowany w API (`GET /api/dispatcher/technicians` pokazuje urządzenie technika).
- Aplikacja **w tle**: powiadomienie „Nowe zgłoszenie” w zasobniku → tapnięcie → aplikacja otwiera szczegóły zgłoszenia.
- Aplikacja **zamknięta** (usunięta z ostatnich, nie „Wymuś zatrzymanie”): tapnięcie → splash → start → szczegóły.
- **Pierwszy plan**: baner w aplikacji „Nowe zgłoszenie: {tytuł}” / „Dyspozytor zmienił status: {tytuł}” z akcją „Pokaż”,
  lista się odświeża; bez powiadomienia systemowego.
- Uwaga: w tym checkpoincie lokalna baza zawiera tylko zgłoszenia utworzone na telefonie — zgłoszenia dyspozytora
  aplikacja pobiera z API dopiero w bloku REST (12-api), więc szczegóły takiego zgłoszenia pokazują „Nie znaleziono zgłoszenia”. Pełny obraz: push na zgłoszenie z listy (plan B /
  `simctl` z `reportId` z listy) albo checkpoint `12-api`.
- iOS (symulator): trzy stany jak wyżej przez `xcrun simctl push`; brak tokenu nie powoduje błędu.
- Wylogowanie wyrejestrowuje urządzenie (`DELETE /api/devices/{token}`); tapnięcie powiadomienia na ekranie logowania nic nie otwiera.
- Testy, z których usunięto `[Ignore]`, przechodzą (`dotnet test` na zielono).
- Ustawienia → zgoda włączona w ustawieniach systemu → powrót do aplikacji: od razu „Włączone” (bez ponownego wejścia na stronę).
- „Nie teraz” → zgoda włączona w ustawieniach systemu → powrót do aplikacji: urządzenie widać w `GET /api/dispatcher/technicians`, push od dyspozytora dochodzi (bez wylogowania i ponownego logowania).

## Typowe problemy
- Android 13+ bez zgody → brak powiadomień; brak kanału → nic się nie pokazuje (Android 8+).
- Emulator bez Google Play → brak tokenu („Kopiuj token FCM” → komunikat o braku tokenu).
- „Wymuś zatrzymanie” (Ustawienia Androida → Aplikacje → FieldReport DEV → „Wymuś zatrzymanie”; to samo robi
  `adb shell am force-stop pl.szkolenie.fieldreport.dev`) = aplikacja w stanie *stopped* — system nie dostarcza jej pushy, dopóki
  użytkownik jej nie uruchomi (to nie błąd aplikacji).
- iOS: `Simulator Target Bundle` / bundle ID ≠ `ApplicationId` konfiguracji; brak zgody na powiadomienia; brak `type`/`reportId` w `.apns`.
- Aktualizacja UI z wątku zdarzenia pluginu bez `MainThread` → wyjątek (adapter wysyła komunikat na wątku UI).

## Jeśli utkniesz
```bash
# odłóż swoje zmiany (git stash), pobierz nowe gałęzie (git fetch) i przejdź do gotowego rozwiązania
git stash && git fetch && git checkout 11-push
```
