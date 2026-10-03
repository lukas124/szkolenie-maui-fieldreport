using System.Reflection;
using CommunityToolkit.Mvvm.ComponentModel;
using FieldReport.Core.Navigation;

namespace FieldReport.Tests.Architecture;

/// <summary>
/// Reguły architektury sprawdzane automatycznie: Core bez MAUI, ViewModel z ≤ 6 zależnościami,
/// ViewModele nie znają typów danych/platformy.
/// </summary>
[TestFixture]
public class ArchitectureTests
{
    private static readonly Assembly Core = typeof(BaseViewModel).Assembly;

    private static IEnumerable<Type> ViewModels() => Core.GetTypes()
        .Where(t => !t.IsAbstract && typeof(ObservableObject).IsAssignableFrom(t) && t.Name.EndsWith("ViewModel"));

    [Test]
    public void Core_DoesNotReferenceMaui()
    {
        var references = Core.GetReferencedAssemblies().Select(a => a.Name!).ToList();
        Assert.That(references.Where(n => n.StartsWith("Microsoft.Maui", StringComparison.Ordinal)), Is.Empty,
            "FieldReport.Core nie referuje MAUI — dzięki temu ViewModele testujemy bez emulatora");
    }

    [TestCaseSource(nameof(ViewModels))]
    public void ViewModel_HasAtMostSixDependencies(Type viewModel)
    {
        var parameters = viewModel.GetConstructors().Max(c => c.GetParameters().Length);
        Assert.That(parameters, Is.LessThanOrEqualTo(6), $"{viewModel.Name}: sygnał ostrzegawczy — więcej niż ~5–6 zależności");
    }

    [TestCaseSource(nameof(ViewModels))]
    public void ViewModel_DoesNotDependOnDataOrHttp(Type viewModel)
    {
        var forbidden = viewModel.GetConstructors().SelectMany(c => c.GetParameters()).Select(p => p.ParameterType)
            .Where(t => t.Namespace?.StartsWith("FieldReport.Core.Data", StringComparison.Ordinal) == true
                        || t == typeof(HttpClient))
            .ToList();
        Assert.That(forbidden, Is.Empty, $"{viewModel.Name}: I/O (repozytorium, HTTP) tylko przez usługi");
    }

    [Test]
    public void EveryPageViewModel_DerivesFromBaseViewModel()
    {
        var pageViewModels = ViewModels().Where(t => t.Namespace is "FieldReport.Core.ViewModels"
                                                     || t.Name == "NewReportFlowViewModel");
        Assert.That(pageViewModels.Where(t => !typeof(BaseViewModel).IsAssignableFrom(t)), Is.Empty);
    }
}
