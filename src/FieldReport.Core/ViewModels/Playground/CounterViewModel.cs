using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace FieldReport.Core.ViewModels.Playground;

/// <summary>Licznik kliknięć (CommunityToolkit.Mvvm): generator dopisuje właściwość Count i komendę IncrementCommand.</summary>
public partial class CounterViewModel : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CountText))]
    private int _count;

    public string CountText => $"Kliknięcia: {Count}";

    [RelayCommand]
    private void Increment() => Count++;
}
