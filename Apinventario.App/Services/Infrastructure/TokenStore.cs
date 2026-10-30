using Apinventario.App.Models.Common;
using System.Text.Json;

namespace Apinventario.App.Services.Infrastructure;

public sealed class TokenStore : ITokenStore
{
    private const string Key = "apinventario-session-v1";
    public async Task<StoredSession?> ReadAsync()
    {
        try
        {
            var text = await SecureStorage.Default.GetAsync(Key);
            return text is null ? null : JsonSerializer.Deserialize<StoredSession>(text);
        }
        catch { Clear(); return null; }
    }
    public Task WriteAsync(StoredSession session) => SecureStorage.Default.SetAsync(Key, JsonSerializer.Serialize(session));
    public void Clear() => SecureStorage.Default.Remove(Key);
}
