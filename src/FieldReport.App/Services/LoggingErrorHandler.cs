using FieldReport.Core.Services;
using Microsoft.Extensions.Logging;

namespace FieldReport.App.Services;

// Błędy z zadań w tle logujemy w kategorii App
public sealed class LoggingErrorHandler(ILogger<App> logger) : IErrorHandler
{
    public void Handle(Exception exception) => logger.LogError(exception, "Unhandled exception in fire-and-forget task");
}
