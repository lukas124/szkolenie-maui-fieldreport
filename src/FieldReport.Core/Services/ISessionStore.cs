using FieldReport.Core.Models;

namespace FieldReport.Core.Services;

/// <summary>Magazyn sesji (tokeny). Implementacja w App: SecureStorage; w testach mock.</summary>
public interface ISessionStore
{
    Task<Session?> GetAsync();
    Task SaveAsync(Session session);
    Task ClearAsync();
}
