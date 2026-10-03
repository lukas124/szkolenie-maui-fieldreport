using System.Text.RegularExpressions;

namespace FieldReport.Core.Services;

public sealed partial class CredentialsValidator : ICredentialsValidator
{
    public const int MinPinLength = 4, MaxPinLength = 6;

    public bool IsValid(string? email, string? pin) => IsValidEmail(email) && IsValidPin(pin);

    private static bool IsValidEmail(string? email) => email is not null && EmailPattern().IsMatch(email.Trim());

    private static bool IsValidPin(string? pin) =>
        pin is { Length: >= MinPinLength and <= MaxPinLength } && pin.All(char.IsAsciiDigit);

    // celowo prosto: coś@coś.domena, bez spacji
    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]{2,}$", RegexOptions.CultureInvariant)]
    private static partial Regex EmailPattern();
}
