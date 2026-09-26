using System.Globalization;

namespace FieldReport.Core.Services;

/// <summary>Imię technika wyprowadzone z e-maila (tak samo jak w API szkoleniowym): „jan.kowalski@firma.pl” → „Jan Kowalski”.</summary>
public static class TechnicianNames
{
    private static readonly CultureInfo Polish = CultureInfo.GetCultureInfo("pl-PL");

    public static string FromEmail(string email)
    {
        var local = email.Split('@')[0];
        var parts = local.Split(['.', '_', '-', '+'], StringSplitOptions.RemoveEmptyEntries);
        return parts.Length == 0
            ? email
            : string.Join(' ', parts.Select(p => char.ToUpper(p[0], Polish) + p[1..].ToLower(Polish)));
    }
}
