using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Apinventario.App.Models.Common;

namespace Apinventario.App.Services.Infrastructure;

public sealed class ApiClient(HttpClient http, ApiConfig config, ITokenStore tokens, AppErrorLogger logger)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    public event Action? Unauthorized;
    public async Task<T> SendJsonAsync<T>(HttpMethod method, string relativePath, object? body = null,
        CancellationToken ct = default, bool authenticated = true)
    {
        using var response = await SendAsync(method, relativePath, body is null ? null : JsonContent.Create(body, options: Json), ct, authenticated);
        try { return await response.Content.ReadFromJsonAsync<T>(Json, ct) ?? throw new JsonException(); }
        catch (JsonException) { await logger.WriteAsync("HTTP", (int)response.StatusCode); throw new ApiFailure((int)response.StatusCode, "La API devolvió un formato inesperado."); }
    }
    public async Task SendNoContentAsync(HttpMethod method, string relativePath, object? body = null, CancellationToken ct = default)
    { using var response = await SendAsync(method, relativePath, body is null ? null : JsonContent.Create(body, options: Json), ct, true); }
    public async Task<T> SendMultipartAsync<T>(string relativePath, MultipartFormDataContent body, CancellationToken ct = default)
    {
        using var response = await SendAsync(HttpMethod.Post, relativePath, body, ct, true);
        return await response.Content.ReadFromJsonAsync<T>(Json, ct) ?? throw new ApiFailure(200, "Respuesta vacía de la API.");
    }
    public async Task<DownloadedFile> DownloadAsync(string relativePath, CancellationToken ct = default)
    {
        using var response = await SendAsync(HttpMethod.Get, relativePath, null, ct, true);
        var mime = response.Content.Headers.ContentType?.MediaType ?? "";
        if (mime != "application/pdf") throw new ApiFailure(200, "La respuesta no es un documento PDF.");
        var bytes = await response.Content.ReadAsByteArrayAsync(ct);
        if (bytes.Length < 5 || System.Text.Encoding.ASCII.GetString(bytes, 0, 5) != "%PDF-")
            throw new ApiFailure(200, "El documento PDF recibido no es válido.");
        return new(bytes, mime, Path.GetFileName(response.Content.Headers.ContentDisposition?.FileNameStar ??
            response.Content.Headers.ContentDisposition?.FileName?.Trim('"') ?? "reporte.pdf"));
    }
    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string path, HttpContent? content, CancellationToken ct, bool authenticated)
    {
        if (Uri.TryCreate(path, UriKind.Absolute, out _ ) || path.Contains("..") || path.StartsWith("//"))
            throw new ArgumentException("Se requiere una ruta relativa de la API.");
        using var request = new HttpRequestMessage(method, new Uri(config.BaseUri, path.TrimStart('/'))) { Content = content };
        if (authenticated)
        {
            var saved = await tokens.ReadAsync();
            if (saved is null || saved.ExpiresAtUtc <= DateTimeOffset.UtcNow)
            { tokens.Clear(); Unauthorized?.Invoke(); throw new ApiFailure(401, "La sesión venció. Iniciá sesión nuevamente."); }
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", saved.Token);
        }
        HttpResponseMessage response;
        try { response = await http.SendAsync(request, ct); }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        { await logger.WriteAsync("Network"); throw new ApiFailure(null, "Se agotó el tiempo de espera. Si estabas guardando, verificá el resultado antes de repetir."); }
        catch (HttpRequestException)
        { await logger.WriteAsync("Network"); throw new ApiFailure(null, "No se pudo conectar con la API. Revisá la conexión y el servidor."); }
        if (response.IsSuccessStatusCode) return response;
        var status = (int)response.StatusCode;
        var message = status switch
        { 401 => "Sesión o credenciales no válidas.", 403 => "Tu rol no tiene permiso para esta acción.", 404 => "El registro ya no está disponible.",
          409 => "Los datos cambiaron. Actualizá antes de repetir.", 413 => "El archivo supera el tamaño permitido.", 429 => "Demasiados intentos. Esperá antes de reintentar.",
          >= 500 => "El servidor no pudo completar la operación.", _ => "Revisá los datos enviados." };
        var errors = new Dictionary<string, string[]>();
        try
        {
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
            if (status < 500 && json.RootElement.TryGetProperty("error", out var error) && error.ValueKind == JsonValueKind.String)
                message = error.GetString() ?? message;
            if (json.RootElement.TryGetProperty("errors", out var fields) && fields.ValueKind == JsonValueKind.Object)
                foreach (var field in fields.EnumerateObject())
                    errors[field.Name] = field.Value.ValueKind == JsonValueKind.Array ? field.Value.EnumerateArray().Select(x => x.GetString() ?? "").ToArray() : [];
        }
        catch (JsonException) { }
        if (status is 400 or 422 && errors.Count > 0)
            message = string.Join(" ", errors.SelectMany(x => x.Value).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct());
        var retry = response.Headers.RetryAfter?.Delta ?? (response.Headers.RetryAfter?.Date - DateTimeOffset.UtcNow);
        response.Dispose();
        await logger.WriteAsync("HTTP", status);
        if (status == 401 && authenticated) { tokens.Clear(); Unauthorized?.Invoke(); }
        throw new ApiFailure(status, message, errors, retry);
    }
    public static string Query(params (string Key, object? Value)[] values) => string.Join("&", values
        .Where(x => x.Value is not null && !string.IsNullOrWhiteSpace(x.Value.ToString()))
        .Select(x => Uri.EscapeDataString(x.Key) + "=" + Uri.EscapeDataString(x.Value is bool b ? b.ToString().ToLowerInvariant() :
            Convert.ToString(x.Value, System.Globalization.CultureInfo.InvariantCulture)!)));
}
