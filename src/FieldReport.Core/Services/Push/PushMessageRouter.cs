using FieldReport.Core.Navigation;

namespace FieldReport.Core.Services.Push;

/// <summary>Payload push (data: <c>type</c>, <c>reportId</c>) → DeepLink. Czysta logika, łatwa do testów.</summary>
public sealed class PushMessageRouter : IPushMessageRouter
{
    public const string ReportAssigned = "report_assigned";
    public const string ReportStatusChanged = "report_status_changed";

    // dane z powiadomienia (same stringi) → nasz link albo null
    public DeepLink? ToDeepLink(IReadOnlyDictionary<string, string> data) =>
        data.TryGetValue("type", out var type) && data.TryGetValue("reportId", out var id) && Guid.TryParse(id, out var reportId)
            ? type switch
            {
                ReportAssigned or ReportStatusChanged => new DeepLink(reportId),
                _ => null                              // nieznany typ
            }
            : null;
}
