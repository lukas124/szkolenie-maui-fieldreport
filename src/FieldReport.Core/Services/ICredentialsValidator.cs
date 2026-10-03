namespace FieldReport.Core.Services;

/// <summary>Reguła formatu danych logowania technika: poprawny e-mail + PIN 4–6 cyfr. „Zaloguj” aktywne tylko przy true.</summary>
public interface ICredentialsValidator
{
    bool IsValid(string? email, string? pin);
}
