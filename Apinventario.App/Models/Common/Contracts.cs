namespace Apinventario.App.Models.Common;

public sealed record UserInfo(int Id, string Nombre, string Apellido, string CUIT, string Email, string Rol, bool Eliminado);
public sealed record LoginResult(string Token, DateTime ExpiresAt, UserInfo Usuario);
public sealed record StoredSession(string Token, DateTimeOffset ExpiresAtUtc);
public sealed class PageResult<T>
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
    public bool HasNext { get; set; }
    public bool HasPrevious { get; set; }
    public List<T> Items { get; set; } = [];
}
public sealed record DownloadedFile(byte[] Bytes, string MimeType, string FileName);
public sealed class ApiFailure(int? statusCode, string message,
    Dictionary<string, string[]>? validationErrors = null, TimeSpan? retryAfter = null) : Exception(message)
{
    public int? StatusCode { get; } = statusCode;
    public Dictionary<string, string[]> ValidationErrors { get; } = validationErrors ?? [];
    public TimeSpan? RetryAfter { get; } = retryAfter;
}
