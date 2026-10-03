using FieldReport.Core.Services;

namespace FieldReport.App.MarkupExtensions;

/// <summary>
/// Rozszerzenie znaczników (markup extension) do tekstów w XAML: <c>Text="{loc:Translate Reports_Title}"</c>.
/// Zwraca Binding do Localizera (z kontenera DI), więc po zmianie języka (SetLocale) tekst zmienia się bez restartu.
/// </summary>
[ContentProperty(nameof(Key))]
[AcceptEmptyServiceProvider]
public sealed class TranslateExtension : IMarkupExtension<BindingBase>
{
    /// <summary>Klucz tekstu w Strings.resx.</summary>
    public string Key { get; set; } = string.Empty;

    public BindingBase ProvideValue(IServiceProvider serviceProvider)
    {
        var localizer = IPlatformApplication.Current!.Services.GetRequiredService<ILocalizer>();
        return new Binding($"[{Key}]", source: localizer); // ścieżka „[Klucz]” = indeksator Localizer[Klucz]
    }

    object IMarkupExtension.ProvideValue(IServiceProvider serviceProvider) => ProvideValue(serviceProvider);
}
