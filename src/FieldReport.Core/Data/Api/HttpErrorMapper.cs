using System.Net;
using FieldReport.Core.Models;

namespace FieldReport.Core.Data.Api;

/// <summary>Kod HTTP → błąd aplikacji (tekst dla użytkownika z zasobów Error_{kod}).</summary>
public static class HttpErrorMapper
{
    public static Error Map(HttpStatusCode status) => status switch
    {
        HttpStatusCode.Unauthorized => Errors.Unauthorized,
        HttpStatusCode.BadRequest or HttpStatusCode.UnprocessableEntity or HttpStatusCode.RequestEntityTooLarge
            or HttpStatusCode.UnsupportedMediaType => Errors.InvalidRequest,
        HttpStatusCode.NotFound => Errors.NotFound,
        HttpStatusCode.Conflict => Errors.Conflict,
        HttpStatusCode.RequestTimeout or HttpStatusCode.TooManyRequests => Errors.Network,
        >= HttpStatusCode.InternalServerError => Errors.Server,
        _ => Errors.Unknown
    };
}
