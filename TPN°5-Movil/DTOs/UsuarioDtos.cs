using System.ComponentModel.DataAnnotations;

namespace TPN_5_MOVIL.DTOs;

public record UsuarioDto(int Id, string Nombre, string Apellido, string CUIT, string Email, string Rol, bool Eliminado);
public record UsuarioCreateRequest(string Nombre, string Apellido, string CUIT, string Email, string Password, string Rol);
public record UsuarioUpdateRequest(string Nombre, string Apellido, [Required, MaxLength(20)] string CUIT, string Email, string? Password);
