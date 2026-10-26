using System.ComponentModel.DataAnnotations;

namespace TPN_5_MOVIL.DTOs;

public record ClienteResponse(int Id, string Nombre, string Apellido, string CUIT, string Email, string? Telefono, string? Direccion);
public record ClienteRequest(string Nombre, string Apellido, [Required, MaxLength(20)] string CUIT, string Email, string? Telefono, string? Direccion);
