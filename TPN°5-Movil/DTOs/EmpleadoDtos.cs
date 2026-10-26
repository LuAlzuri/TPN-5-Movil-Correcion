using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace TPN_5_MOVIL.DTOs;

public record EmpleadoResponse(int Id, string Nombre, string Apellido, string CUIT,
    string Email, DateOnly FechaIngreso);

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public class EmpleadoUpdateRequest
{
    [Required, MaxLength(150)]
    public string Nombre { get; init; } = string.Empty;
    [Required, MaxLength(150)]
    public string Apellido { get; init; } = string.Empty;
    [Required, MaxLength(20)]
    public string CUIT { get; init; } = string.Empty;
    [Required, EmailAddress, MaxLength(150)]
    public string Email { get; init; } = string.Empty;
    [Required]
    public DateOnly? FechaIngreso { get; init; }
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class EmpleadoCreateRequest : EmpleadoUpdateRequest
{
    [Required, MinLength(12), MaxLength(128)]
    public string Password { get; init; } = string.Empty;
}
