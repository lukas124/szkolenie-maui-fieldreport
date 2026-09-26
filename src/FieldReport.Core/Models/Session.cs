namespace FieldReport.Core.Models;

/// <summary>Sesja zalogowanego technika: tokeny + dane do wyświetlenia. Trzymana w SecureStorage (ISessionStore).</summary>
public sealed record Session(
    string Email,
    string TechnicianName,
    string AccessToken,
    string RefreshToken);
