using System.Globalization;
using FieldReport.Core.Models;

namespace FieldReport.App.Converters;

/// <summary>ReportStatus → kolor znacznika statusu.</summary>
public sealed class StatusToColorConverter : IValueConverter
{
    public Color New { get; set; } = Colors.SteelBlue;
    public Color InProgress { get; set; } = Colors.Orange;
    public Color Done { get; set; } = Colors.Green;

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value switch
    {
        ReportStatus.InProgress => InProgress,
        ReportStatus.Done => Done,
        _ => New
    };

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
