using System.ComponentModel.DataAnnotations;

namespace TPN_5_MOVIL.DTOs;

public record CategoriaResponse(int Id, string Nombre, string? Descripcion, DateTime FechaCreacion);

public record CategoriaCatalogoResponse(int Id, string Nombre);

public record CategoriaCreateRequest(
    [Required, MaxLength(100)] string Nombre,
    [MaxLength(1000)] string? Descripcion);

public record CategoriaUpdateRequest(
    [Required, MaxLength(100)] string Nombre,
    [MaxLength(1000)] string? Descripcion);
