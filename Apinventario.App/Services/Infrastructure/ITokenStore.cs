using Apinventario.App.Models.Common;
namespace Apinventario.App.Services.Infrastructure;
public interface ITokenStore
{
    Task<StoredSession?> ReadAsync();
    Task WriteAsync(StoredSession session);
    void Clear();
}
