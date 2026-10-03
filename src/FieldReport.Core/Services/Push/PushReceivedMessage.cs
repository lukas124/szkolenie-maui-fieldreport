namespace FieldReport.Core.Services.Push;

/// <summary>Powiadomienie odebrane, gdy aplikacja jest na pierwszym planie (IMessenger). Body = tytuł zgłoszenia z API.</summary>
public sealed record PushReceivedMessage(IReadOnlyDictionary<string, string> Data, string? Body = null);

/// <summary>Dyspozytor zmienił zgłoszenia (push na pierwszym planie) — otwarta lista ma się odświeżyć.</summary>
public sealed record ReportsChangedMessage;
