namespace FieldReport.Core.ViewModels;

/// <summary>Pozycja listy języków w Ustawieniach: kod kultury (np. „pl-PL”) i nazwa do wyświetlenia (Picker pokazuje ToString()).</summary>
public sealed record LanguageOption(string Code, string Name)
{
    public override string ToString() => Name;
}
