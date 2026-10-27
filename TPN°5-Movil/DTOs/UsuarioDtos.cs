using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace TPN_5_MOVIL.DTOs;

public record UsuarioDto(int Id, string Nombre, string Apellido, string CUIT, string Email, string Rol, bool Eliminado);

public abstract class UsuarioDatosRequest
{
    [Required, MaxLength(150)]
    public string Nombre { get; init; } = string.Empty;
    [Required, MaxLength(150)]
    public string Apellido { get; init; } = string.Empty;
    [Required, MaxLength(20)]
    public string CUIT { get; init; } = string.Empty;

    private string email = string.Empty;
    [Required, EmailAddress, MaxLength(150)]
    public string Email
    {
        get => email;
        init => email = Normalizacion.Email(value);
    }
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class UsuarioCreateRequest : UsuarioDatosRequest
{
    [Required, MinLength(12), MaxLength(128)]
    public string Password { get; init; } = string.Empty;
    [Required]
    public string Rol { get; init; } = string.Empty;
    public DateOnly? FechaIngreso { get; init; }
    [MaxLength(50)]
    public string? Telefono { get; init; }
    [MaxLength(300)]
    public string? Direccion { get; init; }
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class UsuarioUpdateRequest : UsuarioDatosRequest
{
    [MinLength(12), MaxLength(128)]
    public string? Password { get; init; }
}
