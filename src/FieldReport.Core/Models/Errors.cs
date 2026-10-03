namespace FieldReport.Core.Models;

/// <summary>Katalog błędów aplikacji. Nowy błąd = nowy kod + tekst w zasobach (<c>Error_{kod}</c>).</summary>
public static class Errors
{
    public static readonly Error Unknown = new("unknown");
    public static readonly Error Network = new("network");
    public static readonly Error InvalidTitle = new("invalid_title");
    public static readonly Error InvalidDescription = new("invalid_description");
    public static readonly Error ReportNotFound = new("report_not_found");
    public static readonly Error ReportAlreadySynced = new("report_already_synced");
    public static readonly Error InvalidStatusTransition = new("invalid_status_transition");
    public static readonly Error InvalidCredentials = new("invalid_credentials");

    // HTTP / API (HttpErrorMapper)
    public static readonly Error Unauthorized = new("unauthorized");
    public static readonly Error SessionExpired = new("session_expired");
    public static readonly Error InvalidRequest = new("invalid_request");
    public static readonly Error NotFound = new("not_found");
    public static readonly Error Conflict = new("conflict");
    public static readonly Error Server = new("server");
    public static readonly Error InvalidResponse = new("invalid_response");

    // powiadomienia push
    public static readonly Error PushTokenUnavailable = new("push_token_unavailable");

    // uprawnienia: odmowa (można zapytać ponownie) vs odmowa na stałe (tylko ustawienia systemu)
    public static readonly Error PermissionDenied = new("permission_denied");
    public static readonly Error PermissionDeniedPermanently = new("permission_denied_permanently");

    /// <summary>Aparat i galeria (kreator nowego zgłoszenia, krok „Zdjęcie”).</summary>
    public static class Media
    {
        /// <summary>Użytkownik anulował — to nie jest błąd do pokazania.</summary>
        public static readonly Error Cancelled = new("media_cancelled");
        public static readonly Error NotSupported = new("media_not_supported");
        public static readonly Error Failed = new("media_failed");
    }
}
