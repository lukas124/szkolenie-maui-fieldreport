namespace FieldReport.Core.Services;

/// <summary>Obsługa wyjątków nieoczekiwanych (np. z fire-and-forget): log zamiast cichej utraty błędu.</summary>
public interface IErrorHandler
{
    void Handle(Exception exception);
}
