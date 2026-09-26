namespace FieldReport.Core.ViewModels.NewReport;

/// <summary>Czysta logika indeksu kroków kreatora (bez UI, łatwa do testów).</summary>
public sealed class StepSequence(int count)
{
    public int Index { get; private set; }
    public int Count => count;
    public bool IsFirst => Index == 0;
    public bool IsLast => Index == count - 1;
    public double Progress => (Index + 1d) / count; // 1/3 …
    public bool MoveNext() { if (IsLast) return false; Index++; return true; }
    public bool MovePrevious() { if (IsFirst) return false; Index--; return true; }
}
