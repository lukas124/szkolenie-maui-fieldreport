using FieldReport.Core.Models;

namespace FieldReport.Core.ValueObjects;

/// <summary>
/// Tytuł zgłoszenia (value object): nie da się utworzyć niepoprawnego tytułu.
/// Reguła „3–80 znaków, bez spacji na brzegach” jest tylko tutaj, a nie w ViewModelu czy XAML.
/// </summary>
public sealed record ReportTitle
{
    public const int MinLength = 3, MaxLength = 80;

    private ReportTitle(string value) => Value = value; // tylko przez Create

    public string Value { get; }

    public static Result<ReportTitle> Create(string? raw)
    {
        var value = raw?.Trim() ?? string.Empty;   // bez spacji na brzegach
        return value.Length is < MinLength or > MaxLength
            ? Result<ReportTitle>.Failure(Errors.InvalidTitle)
            : Result<ReportTitle>.Success(new ReportTitle(value));
    }

    public override string ToString() => Value;
}
