using FieldReport.Core.Navigation;

namespace FieldReport.Core.ViewModels.Playground;

/// <summary>ViewModel strony Playground: deleguje do ViewModelu komponentu (licznik).</summary>
public sealed class PlaygroundViewModel(INavigationService navigation, CounterViewModel counter) : BaseViewModel(navigation)
{
    public CounterViewModel Counter { get; } = counter;
}
