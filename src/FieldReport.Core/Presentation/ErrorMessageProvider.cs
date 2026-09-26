using FieldReport.Core.Models;
using FieldReport.Core.Services;

namespace FieldReport.Core.Presentation;

/// <summary>Zamienia kod błędu na tekst z zasobów (klucz <c>Error_{kod}</c>). Nowy błąd = nowy wpis w Strings.resx, bez zmian w kodzie.</summary>
public sealed class ErrorMessageProvider(ILocalizer localizer) : IErrorMessageProvider
{
    public string GetMessage(Error error)
    {
        var key = $"Error_{error.Code}";        // np. Error_network
        var text = localizer.GetString(key);    // brak tłumaczenia → zwraca sam klucz
        return text != key
            ? text                                 // znany → np. „Brak połączenia z internetem”
            : localizer.GetString("Error_unknown"); // nieznany błąd → „Coś poszło nie tak”
    }
}
