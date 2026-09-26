namespace FieldReport.Core.Models;

/// <summary>Katalog błędów aplikacji. Nowy błąd = nowy kod + tekst w zasobach (<c>Error_{kod}</c>).</summary>
public static class Errors
{
    public static readonly Error Unknown = new("unknown");
    public static readonly Error Network = new("network");
    public static readonly Error InvalidTitle = new("invalid_title");
    public static readonly Error ReportNotFound = new("report_not_found");
    public static readonly Error ReportAlreadySynced = new("report_already_synced");
}
