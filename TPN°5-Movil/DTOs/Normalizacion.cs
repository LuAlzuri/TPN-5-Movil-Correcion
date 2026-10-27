namespace TPN_5_MOVIL.DTOs;

public static class Normalizacion
{
    public static string Email(string? value) => value?.Trim().ToLowerInvariant() ?? string.Empty;
    public static string? Opcional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    public static string? EmailOpcional(string? value) => Opcional(value)?.ToLowerInvariant();
}
