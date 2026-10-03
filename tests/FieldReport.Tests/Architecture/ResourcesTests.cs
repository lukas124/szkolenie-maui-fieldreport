using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using FieldReport.Core.Models;
using FieldReport.Core.Services.Push;

namespace FieldReport.Tests.Architecture;

/// <summary>
/// Każdy tekst dla użytkownika ma wpis w Strings.resx (brak = sam klucz na ekranie), a tłumaczenie Strings.en.resx
/// ma dokładnie te same klucze. Pliki resx są w projekcie App, więc czytamy je jako XML.
/// </summary>
[TestFixture]
public partial class ResourcesTests
{
    private static readonly string Src =
        Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "../../../../../src"));

    private static readonly string Localizations = Path.Combine(Src, "FieldReport.App/Resources/Localizations");

    private static HashSet<string> KeysIn(string fileName) =>
        XDocument.Load(Path.Combine(Localizations, fileName)).Root!.Elements("data")
            .Select(d => (string)d.Attribute("name")!).ToHashSet();

    private HashSet<string> _keys = null!;

    [SetUp]
    public void SetUp()
    {
        Assume.That(Directory.Exists(Localizations), "Źródła niedostępne (test uruchomiony poza repozytorium)");
        _keys = KeysIn("Strings.resx");
    }

    private static IEnumerable<Error> AllErrors() =>
        new[] { typeof(Errors) }.Concat(typeof(Errors).GetNestedTypes())
            .SelectMany(t => t.GetFields(BindingFlags.Public | BindingFlags.Static))
            .Where(f => f.FieldType == typeof(Error))
            .Select(f => (Error)f.GetValue(null)!);

    [TestCaseSource(nameof(AllErrors))]
    public void EveryError_HasMessage(Error error) =>
        Assert.That(_keys, Does.Contain($"Error_{error.Code}"), $"Brak Error_{error.Code} w Strings.resx");

    [Test]
    public void EveryEnumValue_HasText()
    {
        var keys = Enum.GetNames<ReportStatus>().Select(n => $"ReportStatus_{n}")
            .Concat(Enum.GetNames<ReportPriority>().Select(n => $"ReportPriority_{n}"))
            .Concat(Enum.GetNames<SyncState>().Select(n => $"SyncState_{n}"))
            .Concat(new[] { ReportStatus.InProgress, ReportStatus.Done }.Select(s => $"StatusAction_{s}"))
            .Concat(new[] { PushMessageRouter.ReportAssigned, PushMessageRouter.ReportStatusChanged }.Select(t => $"Push_{t}"));
        Assert.That(keys.Where(k => !_keys.Contains(k)), Is.Empty);
    }

    [Test]
    public void EnglishTranslation_HasTheSameKeys()
    {
        var english = KeysIn("Strings.en.resx");

        Assert.That(_keys.Except(english).Order(), Is.Empty, "Brak tłumaczenia w Strings.en.resx");
        Assert.That(english.Except(_keys).Order(), Is.Empty, "Klucz tylko w Strings.en.resx");
    }

    [Test]
    public void EveryKeyUsedInCodeAndXaml_Exists()
    {
        var used = new HashSet<string>();
        foreach (var file in Directory.EnumerateFiles(Src, "*.*", SearchOption.AllDirectories)
                     .Where(f => (f.EndsWith(".cs") || f.EndsWith(".xaml")) && !f.Contains("/obj/") && !f.Contains("/bin/")))
        {
            var text = File.ReadAllText(file);
            foreach (System.Text.RegularExpressions.Match m in XamlKey().Matches(text)) used.Add(m.Groups[1].Value);
            foreach (System.Text.RegularExpressions.Match m in CodeKey().Matches(text)) used.Add(m.Groups[1].Value);
        }

        Assert.That(used, Is.Not.Empty);
        Assert.That(used.Where(k => !_keys.Contains(k)).Order(), Is.Empty);
    }

    [GeneratedRegex(@"\{loc:Translate ([A-Za-z]+_[A-Za-z_]+)\}")]
    private static partial Regex XamlKey();

    // literały w stylu „Settings_SignOutTitle” przekazywane do ILocalizer / IUserFeedback
    [GeneratedRegex(@"""((?:Common|Reports|Details|NewReport|Settings|Login|Startup|StartupError|Permission|Priming|Push)_[A-Za-z]+)""")]
    private static partial Regex CodeKey();
}
