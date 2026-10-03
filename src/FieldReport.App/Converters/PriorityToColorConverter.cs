using System.Globalization;

namespace FieldReport.App.Converters;

/// <summary>ViewModel wystawia znaczenie (ReportPriority), konwerter — wygląd. Kolory przychodzą z zasobów.</summary>
public sealed class PriorityToColorConverter : IValueConverter
{
    public Color High { get; set; } = Colors.Red;
    public Color Normal { get; set; } = Colors.Orange;
    public Color Low { get; set; } = Colors.Gray;

    // TODO: ReportPriority.High → High, Normal → Normal, pozostałe → Low (wzór: StatusToColorConverter)
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => Low;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
