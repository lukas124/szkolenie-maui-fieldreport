namespace FieldReport.Core.Services.Push;

public enum PushPermissionStatus
{
    /// <summary>Jeszcze nie pytaliśmy (albo można zapytać ponownie).</summary>
    NotDetermined,
    Granted,
    /// <summary>Odmowa na stałe — zmienić można tylko w ustawieniach systemu.</summary>
    DeniedPermanently
}
