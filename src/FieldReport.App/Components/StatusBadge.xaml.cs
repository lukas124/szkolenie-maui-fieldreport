using FieldReport.Core.Models;

namespace FieldReport.App.Components;

public partial class StatusBadge : ContentView
{
    public static readonly BindableProperty StatusProperty = BindableProperty.Create(
        nameof(Status), typeof(ReportStatus), typeof(StatusBadge), ReportStatus.New);

    public static readonly BindableProperty TextProperty = BindableProperty.Create(
        nameof(Text), typeof(string), typeof(StatusBadge), string.Empty);

    public StatusBadge()
    {
        InitializeComponent();
    }

    public ReportStatus Status
    {
        get => (ReportStatus)GetValue(StatusProperty);
        set => SetValue(StatusProperty, value);
    }

    /// <summary>Gotowy tekst (z IReportFormatter), np. „W trakcie”.</summary>
    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }
}
