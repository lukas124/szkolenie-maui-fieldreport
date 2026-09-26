# Ćwiczenie 10 — SQLite zamiast listy w pamięci

**Blok:** 10 · SQLite i offline-first · dzień 2 · **Start:** gałąź `10-sqlite-start` · **Czas:** 15 min

## Cel
Zgłoszenia zapisujesz w pliku bazy SQLite zamiast w liście w pamięci RAM, która znika po zamknięciu aplikacji —
przetrwają restart: `SqliteReportRepository` zamiast `InMemoryReportRepository`,
podmiana **tylko** w rejestracji DI. Zero zmian w ViewModelach — testy zielone („aha”).

## Co jest w gałęzi startowej
- Pakiety (Core): `sqlite-net-pcl`, `SQLitePCLRaw.bundle_green` (+ przypięte poprawione biblioteki natywne
  `SQLitePCLRaw.lib.e_sqlite3*` 2.1.13 w `Directory.Packages.props` — ostrzeżenie NU1903 o luce w 2.1.11)
- `Core/Data`: `ReportRow` (tabela `reports`), `SqliteMigrations` (`PRAGMA user_version` + lista kroków),
  `SqliteReportRepository(IAppPaths)` z leniwym otwarciem bazy (`DbAsync()`), `GetAsync` i `DeleteAsync`
- `IAppPaths` (Core) → `MauiAppPaths` (App: `FileSystem.AppDataDirectory`), zarejestrowane w `AddPlatformServices()`;
  `PhotoStorage` też korzysta z `IAppPaths`
- W bazie będą zgłoszenia zapisane w telefonie; wszystkie mają stan „Oczekuje na wysłanie” (wysyłkę do API dodamy w bloku REST)

## Kroki
Przejdź na gałąź startową ćwiczenia. Trener udostępnia gałęzie stopniowo, więc najpierw `git fetch` pobiera te,
które właśnie się pojawiły (masz lokalne zmiany i Git nie pozwala zmienić gałęzi? Najpierw `git stash`):

```bash
# pobierz nowe gałęzie i przejdź na start ćwiczenia
git fetch && git checkout 10-sqlite-start
```

W gałęzi startowej miejsca do uzupełnienia są oznaczone `TODO:` (lista: Task List w Visual Studio, TODO w Riderze
albo `git grep -n "TODO:"`). Testy rozwiązania mają atrybut `[Ignore]` — usuń go, gdy uzupełnisz TODO.

1. `ReportRowMapper.ToModel/ToRow` (TODO): enum ↔ int, `ReportTitle.Create(row.Title).Value`, zgłoszenie z bazy ma `SyncState.Pending`.
2. `SqliteReportRepository` (TODO): `GetAllAsync` — `Table<ReportRow>().OrderByDescending(r => r.UpdatedAt)` + mapper;
   `SaveAsync` — `InsertOrReplaceAsync(ReportRowMapper.ToRow(report))`.
3. Podmień rejestrację w `AddCoreServices()` (TODO):
   ```csharp
   services.AddSingleton<IReportRepository, SqliteReportRepository>(); // zamiast InMemoryReportRepository
   ```
   `InMemoryReportRepository` i `SampleReports` nie są już potrzebne — usuń je razem z ich testem.
4. **Testy:** usuń `[NUnit.Framework.Ignore]` z `SqliteReportRepositoryTests` (pełna nazwa atrybutu, bo SQLite też ma `Ignore`),
   uruchom `dotnet test` — testy ViewModeli przechodzą bez zmian.

## Kryteria akceptacji
- Po pierwszym uruchomieniu lista jest pusta („Brak zgłoszeń.”).
- Zgłoszenie z kreatora (także ze zdjęciem) i zmiana statusu w szczegółach **przetrwają ubicie procesu**
  (`adb shell am force-stop pl.szkolenie.fieldreport.dev` → ponowne uruchomienie).
- Usunięcie niewysłanego zgłoszenia też jest trwałe.
- Zmiany są tylko w `Core/Data` i w rejestracji DI (`AddCoreServices()`) — ViewModele i XAML bez zmian.
- `dotnet test` na zielono (testy `SqliteReportRepositoryTests` działają na prawdziwym SQLite w katalogu tymczasowym).

## Podpowiedzi
- Gdzie trzymać dane: `Preferences` (proste ustawienia), `SecureStorage` (tokeny), pliki (zdjęcia), SQLite (dane i zapytania).
- Singleton repozytorium = jedno połączenie; dwa pierwsze zapytania naraz nie otworzą bazy dwa razy (`SemaphoreSlim`).
- Wiersz tabeli ≠ model: model pilnuje reguł (np. `ReportTitle`), wiersz to typy proste.
- `InsertOrReplace` + `Guid` od aplikacji = idempotentny zapis (ta sama idea co `PUT /reports/{id}` w bloku REST).
- Migracje: numer wersji schematu jest w bazie (`PRAGMA user_version`), a `SqliteMigrations` wykonuje tylko brakujące kroki.
  `CreateTableAsync` doda brakującą kolumnę, ale nie zmieni typu ani nie uzupełni danych — to robią kolejne kroki.
  Nowy krok = nowy element **na końcu** listy (przykład: krok 2 w bloku REST dodaje kolumnę `IsOnServer`).
- Podgląd bazy z emulatora (kopiuje plik bazy z katalogu aplikacji na komputer; otwórz np. w DB Browser for SQLite):
  ```bash
  adb exec-out run-as pl.szkolenie.fieldreport.dev cat files/fieldreport.db3 > fieldreport.db3
  ```
- EF Core + SQLite też działa na mobile — wygodniejsze, ale większa binarka i dłuższy start.

## Jeśli utkniesz
```bash
# odłóż swoje zmiany (git stash), pobierz nowe gałęzie (git fetch) i przejdź do gotowego rozwiązania
git stash && git fetch && git checkout 10-sqlite
```
