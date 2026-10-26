using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace TPN_5_MOVIL.DTOs;

public record ClienteResponse(int Id, string Nombre, string Apellido, string CUIT, string Email, string? Telefono, string? Direccion);

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public class ClienteUpdateRequest
{
    [Required, MaxLength(150)]
    public string Nombre { get; init; } = string.Empty;
    [Required, MaxLength(150)]
    public string Apellido { get; init; } = string.Empty;
    [Required, MaxLength(20)]
    public string CUIT { get; init; } = string.Empty;
    [Required, EmailAddress, MaxLength(150)]
    public string Email { get; init; } = string.Empty;
    [MaxLength(50)]
    public string? Telefono { get; init; }
    [MaxLength(300)]
    public string? Direccion { get; init; }
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class ClienteCreateRequest : ClienteUpdateRequest
{
    [Required, MinLength(12), MaxLength(128)]
    public string Password { get; init; } = string.Empty;
}
