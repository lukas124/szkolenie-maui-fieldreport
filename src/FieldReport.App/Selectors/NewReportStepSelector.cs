using FieldReport.Core.ViewModels.NewReport;

namespace FieldReport.App.Selectors;

/// <summary>Wybór widoku kroku kreatora. Selektor to kod widoku (App), ViewModel nie zna DataTemplate.</summary>
/// <remarks>Zwykłe <c>set</c> zamiast <c>required … init</c>: generator XAML w MAUI 10 przypisuje właściwości po utworzeniu obiektu
/// (<c>init</c> = błąd CS8852, <c>required</c> = ostrzeżenie MAUIX2005).</remarks>
public sealed class NewReportStepSelector : DataTemplateSelector
{
    public DataTemplate Details { get; set; } = null!;
    public DataTemplate Photo { get; set; } = null!;
    public DataTemplate Summary { get; set; } = null!;

    // typ ViewModelu kroku → szablon wyglądu
    protected override DataTemplate OnSelectTemplate(object item, BindableObject container) => item switch
    {
        DetailsStepViewModel => Details,
        PhotoStepViewModel => Photo,
        SummaryStepViewModel => Summary,
        _ => throw new ArgumentOutOfRangeException(nameof(item), item.GetType().Name, "No template for step")
    };
}
