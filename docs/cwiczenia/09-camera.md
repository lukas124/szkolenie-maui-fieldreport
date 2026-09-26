# Ćwiczenie 09 — Krok „Zdjęcie” w kreatorze

**Blok:** 09 · API urządzenia, uprawnienia, aparat · dzień 2 · **Start:** gałąź `09-camera-start` · **Czas:** 17 min

## Cel
Krok „Zdjęcie” w kreatorze: zrób / wybierz z galerii / usuń + podgląd. Platforma (MediaPicker, Permissions, pliki)
schowana w usługach App za interfejsami z Core — ViewModel kroku tylko orkiestruje i reaguje na `Result`.

## Co jest w gałęzi startowej
- Core: `MediaFile`, `IMediaService` (`CapturePhotoAsync`, `PickPhotoAsync`), `IPhotoStorage`,
  `IPermissionService` (`EnsureCameraAsync`, `OpenAppSettings`); błędy `Errors.Media.Cancelled / NotSupported / Failed`, `Errors.PermissionDenied`,
  `Errors.PermissionDeniedPermanently`; `IUserFeedback.ConfirmAsync(title, message, accept, cancel)`
- App: `MauiPermissionService` (odmowa vs odmowa na stałe), `MauiMediaService` (MediaPicker, `RotateImage` wg EXIF),
  `PhotoStorage` (skalowanie do 1600 px, JPEG 85% w `AppDataDirectory/photos`)
- `PhotoStepViewModel` (pomocnicze `SetPhoto` i `HandleErrorAsync` gotowe, komendy do napisania) + szablon kroku
  (podgląd `Image`, trzy przyciski), zdjęcie w podsumowaniu, na liście i w szczegółach

## Kroki
Przejdź na gałąź startową ćwiczenia. Trener udostępnia gałęzie stopniowo, więc najpierw `git fetch` pobiera te,
które właśnie się pojawiły (masz lokalne zmiany i Git nie pozwala zmienić gałęzi? Najpierw `git stash`):

```bash
# pobierz nowe gałęzie i przejdź na start ćwiczenia
git fetch && git checkout 09-camera-start
```

W gałęzi startowej miejsca do uzupełnienia są oznaczone `TODO:` (lista: Task List w Visual Studio, TODO w Riderze
albo `git grep -n "TODO:"`). Testy rozwiązania mają atrybut `[Ignore]` — usuń go, gdy uzupełnisz TODO.

1. Manifesty (TODO): `Platforms/Android/AndroidManifest.xml` — `CAMERA`, `uses-feature camera required=false`,
   `READ_EXTERNAL_STORAGE` (`maxSdkVersion=32`), `<queries>` dla `IMAGE_CAPTURE`; `Platforms/iOS/Info.plist` —
   `NSCameraUsageDescription`, `NSPhotoLibraryUsageDescription`. Bez opisów w `Info.plist` iOS zamyka aplikację przy pierwszym użyciu aparatu.
2. Komendy w `PhotoStepViewModel` (TODO):
   ```csharp
   [RelayCommand]
   private async Task CapturePhotoAsync(CancellationToken ct)
   {
       var result = await _media.CapturePhotoAsync(ct);
       if (result.IsSuccess) { SetPhoto(result.Value!); return; }
       await HandleErrorAsync(result.Error!); // Cancelled → nic; na stałe → wyjaśnienie + ustawienia; reszta → ShowErrorAsync
   }
   ```
   analogicznie `PickPhotoAsync`, oraz `RemovePhoto` (`CanExecute = nameof(HasPhoto)`): czyści `_draft.PhotoPath` i `PhotoPath`.
3. **Testy:** usuń atrybut `[Ignore]` z klasy `PhotoStepViewModelTests`:
   - `Errors.Media.Cancelled` → brak komunikatu (`ShowErrorAsync` `Times.Never`)
   - `Errors.PermissionDenied` → `ShowErrorAsync` raz
   - odmowa na stałe → `ConfirmAsync(...)` + `IPermissionService.OpenAppSettings()`; „Usuń” czyści draft

## Kryteria akceptacji
- „Zrób zdjęcie” → systemowe pytanie o aparat → aparat; po zrobieniu podgląd w kroku, „Usuń” widoczne.
- Anulowanie aparatu (albo wyjście z galerii bez wyboru) nie pokazuje komunikatu i nie kasuje wcześniejszego zdjęcia.
- Pierwsza odmowa → komunikat „Aplikacja nie ma dostępu do aparatu…”. Druga odmowa (Android: „nie pytaj ponownie”)
  → dialog „Brak dostępu do aparatu” z przyciskiem „Otwórz ustawienia” → ekran ustawień aplikacji.
- „Wybierz z galerii” → systemowy selektor zdjęć (Android 13+: bez uprawnień do całej galerii).
- Duże zdjęcie (np. 6000×6000 px, 4 MB) zapisuje się jako ≤ 1600 px (kilkaset KB).
- Zdjęcie widać w podsumowaniu, na karcie listy i w szczegółach. Zdjęcie jest opcjonalne („Dalej” zawsze aktywne).
- Brak crasha przy braku aparatu (`uses-feature … required=false`, `Errors.Media.NotSupported`).
- Testy, z których usunięto `[Ignore]`, przechodzą (`dotnet test` na zielono).

## Emulator
- Aparat: AVD z `hw.camera.back=virtualscene` (wirtualna scena), w aparacie „migawka” → „Done” (✓).
- Galeria: `adb push usterka.jpg /sdcard/Pictures/ && adb shell am broadcast -a android.intent.action.MEDIA_SCANNER_SCAN_FILE -d file:///sdcard/Pictures/usterka.jpg`
- Reset odmowy: `adb shell pm reset-permissions -p pl.szkolenie.fieldreport.dev` (albo odinstalowanie aplikacji).

## Podpowiedzi
- API urządzenia (`MediaPicker`, `Permissions`, `FileSystem`, `AppInfo`) używamy w ViewModelach **zawsze przez
  interfejs** — test kroku nie potrzebuje emulatora.
- Android: po drugiej odmowie `Permissions.RequestAsync` od razu zwraca `Denied` (bez okna), a
  `ShouldShowRationale` = `false` → to sygnał „odmowa na stałe”. iOS pyta tylko raz.
- W tym checkpoincie zgłoszenia są jeszcze w pamięci (restart = dane przykładowe) — trwały zapis w bloku SQLite.

## Jeśli utkniesz
```bash
# odłóż swoje zmiany (git stash), pobierz nowe gałęzie (git fetch) i przejdź do gotowego rozwiązania
git stash && git fetch && git checkout 09-camera
```
