namespace FieldReport.Core.Services.Push;

/// <summary>Kiedy prosić o zgodę na powiadomienia: po PIERWSZYM logowaniu (ekran wyjaśniający), nigdy przy zimnym starcie.</summary>
public interface INotificationOnboarding
{
    /// <summary>
    /// Wołane po zalogowaniu. Pierwsze logowanie: zapamiętuje, że ekran „Włącz powiadomienia” był pokazany, i zwraca true
    /// (pokaż go). Kolejne logowania: rejestruje urządzenie, jeśli zgoda już jest, i zwraca false.
    /// </summary>
    Task<bool> AfterSignInAsync(CancellationToken ct = default);
}
