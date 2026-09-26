using System.Text.Json;
using FieldReport.Core.Models;
using FieldReport.Core.Services;
using Microsoft.Extensions.Logging;

namespace FieldReport.App.Services;

/// <summary>Sesja (tokeny) w SecureStorage: Android Keystore / iOS Keychain. Nigdy w Preferences.</summary>
public sealed class SecureSessionStore(ILogger<SecureSessionStore> logger) : ISessionStore
{
    private const string Key = "fieldreport.session";

    public async Task<Session?> GetAsync()
    {
        try
        {
            var json = await SecureStorage.Default.GetAsync(Key);
            return json is null ? null : JsonSerializer.Deserialize<Session>(json);
        }
        catch (Exception ex) // uszkodzony magazyn (np. po przywróceniu kopii zapasowej) = brak sesji, nie crash
        {
            logger.LogWarning(ex, "Session could not be read, signing out");
            SecureStorage.Default.Remove(Key);
            return null;
        }
    }

    public Task SaveAsync(Session session) => SecureStorage.Default.SetAsync(Key, JsonSerializer.Serialize(session));

    public Task ClearAsync()
    {
        SecureStorage.Default.Remove(Key);
        return Task.CompletedTask;
    }
}
