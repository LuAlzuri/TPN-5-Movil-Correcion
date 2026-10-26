using System.ComponentModel.DataAnnotations;

namespace TPN_5_MOVIL.DTOs;

public record ProveedorResponse(
    int Id,
    string Nombre,
    string CUIT,
    string? Email,
    string? Telefono,
    string? Direccion);

public record ProveedorRequest(
    string Nombre,
    [Required, MaxLength(20)] string CUIT,
    string? Email,
    string? Telefono,
    string? Direccion);
