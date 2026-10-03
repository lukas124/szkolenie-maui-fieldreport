using FieldReport.Core.Models;

namespace FieldReport.App.Components;

public partial class PriorityBadge : ContentView
{
    public static readonly BindableProperty PriorityProperty = BindableProperty.Create(
        nameof(Priority), typeof(ReportPriority), typeof(PriorityBadge), ReportPriority.Normal);

    public static readonly BindableProperty TextProperty = BindableProperty.Create(
        nameof(Text), typeof(string), typeof(PriorityBadge), string.Empty);

    public PriorityBadge()
    {
        InitializeComponent();
    }

    public ReportPriority Priority
    {
        get => (ReportPriority)GetValue(PriorityProperty);
        set => SetValue(PriorityProperty, value);
    }

    /// <summary>Gotowy tekst (z IReportFormatter), np. „Wysoki”.</summary>
    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }
}
// użycie: <components:PriorityBadge Priority="{Binding Priority}" Text="{Binding PriorityText}" />
