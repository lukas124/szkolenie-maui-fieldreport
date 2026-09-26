namespace FieldReport.App.Behaviors;

/// <summary>
/// Behavior (zachowanie dołączane do kontrolki w XAML) dla pola tekstowego: gdy długość tekstu
/// (bez spacji na brzegach) wypada poza zakresem MinLength–MaxLength, ramka pola (<see cref="TargetBorder"/>)
/// zmienia kolor na <see cref="InvalidStrokeColor"/>. Tekst zostaje w normalnym kolorze.
/// To tylko podpowiedź wizualna — regułę i tak sprawdza model (np. ReportTitle.Create).
/// </summary>
public sealed class TextLengthValidationBehavior : Behavior<Entry>
{
    public int MinLength { get; set; }
    public int MaxLength { get; set; } = int.MaxValue;

    /// <summary>Ramka (Border), w której leży pole; w XAML: <c>TargetBorder="{x:Reference TitleFrame}"</c>.</summary>
    public Border? TargetBorder { get; set; }

    /// <summary>Kolor ramki, gdy długość tekstu jest niepoprawna.</summary>
    public Color InvalidStrokeColor { get; set; } = Colors.Red;

    // MAUI wywołuje tę metodę, gdy behavior zostaje dodany do kontrolki (Entry.Behaviors) —
    // tutaj przy tworzeniu strony, gdy InitializeComponent buduje ją z XAML.
    // Podpinamy OnTextChanged pod TextChanged: od teraz każda zmiana tekstu sprawdza długość.
    protected override void OnAttachedTo(Entry entry)
    {
        base.OnAttachedTo(entry);
        entry.TextChanged += OnTextChanged;
    }

    // MAUI wywołuje tę metodę, gdy behavior jest usuwany z kontrolki: Entry.Behaviors.Remove(...) lub Clear().
    // Zamknięcie strony NIE wywołuje tej metody.
    // Stronę, pole i behavior sprząta Garbage Collector, gdy nic ich już nie trzyma.
    // Odpinamy OnTextChanged od TextChanged: pole przestaje wywoływać behavior.
    protected override void OnDetachingFrom(Entry entry)
    {
        entry.TextChanged -= OnTextChanged;
        base.OnDetachingFrom(entry);
    }

    private void OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (TargetBorder is null) return;

        var length = e.NewTextValue?.Trim().Length ?? 0;
        var isInvalid = length > 0 && (length < MinLength || length > MaxLength); // puste pole nie jest jeszcze błędem

        if (isInvalid) TargetBorder.Stroke = InvalidStrokeColor;
        else TargetBorder.ClearValue(Border.StrokeProperty); // wraca kolor ramki ze stylu InputFrame (jasny/ciemny motyw)
    }
}
