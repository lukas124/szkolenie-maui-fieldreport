namespace FieldReport.Core.Navigation;

/// <summary>Cel nawigacji spoza aplikacji (np. tapnięcie powiadomienia push): zgłoszenie do pokazania.</summary>
public sealed record DeepLink(Guid ReportId);
