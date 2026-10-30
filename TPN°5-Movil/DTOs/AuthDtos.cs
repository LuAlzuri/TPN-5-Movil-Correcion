using System.ComponentModel.DataAnnotations;

namespace TPN_5_MOVIL.DTOs;

public sealed class LoginRequest
{
    [Required, EmailAddress, MaxLength(150)]
    public string Email { get; init; } = string.Empty;
    [Required]
    public string Password { get; init; } = string.Empty;
}

public sealed class RegisterRequest
{
    [Required, MaxLength(150)]
    public string Nombre { get; init; } = string.Empty;
    [Required, MaxLength(150)]
    public string Apellido { get; init; } = string.Empty;
    [Required, MaxLength(20)]
    public string CUIT { get; init; } = string.Empty;
    [Required, EmailAddress, MaxLength(150)]
    public string Email { get; init; } = string.Empty;
    [Required, MinLength(12), MaxLength(128)]
    public string Password { get; init; } = string.Empty;
    [Required]
    public string Rol { get; init; } = "Cliente";
    public DateOnly? FechaIngreso { get; init; }
    [MaxLength(50)]
    public string? Telefono { get; init; }
    [MaxLength(300)]
    public string? Direccion { get; init; }
}

public record LoginResponse(string Token, DateTime ExpiresAt, UsuarioDto Usuario);
