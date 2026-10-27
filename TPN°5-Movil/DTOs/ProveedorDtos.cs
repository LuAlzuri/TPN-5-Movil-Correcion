using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace TPN_5_MOVIL.DTOs;

public record ProveedorResponse(
    int Id,
    string Nombre,
    string CUIT,
    string? Email,
    string? Telefono,
    string? Direccion);

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class ProveedorRequest
{
    [Required, MaxLength(150)]
    public string Nombre { get; init; } = string.Empty;
    [Required, MaxLength(20)]
    public string CUIT { get; init; } = string.Empty;

    private readonly string? email;
    [EmailAddress, MaxLength(150)]
    public string? Email
    {
        get => email;
        init => email = Normalizacion.EmailOpcional(value);
    }

    [MaxLength(50)]
    public string? Telefono { get; init; }
    [MaxLength(300)]
    public string? Direccion { get; init; }
}
