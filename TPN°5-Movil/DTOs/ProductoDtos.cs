namespace TPN_5_MOVIL.DTOs;

public record ProductoResponse(
    int Id,
    string Nombre,
    string? Descripcion,
    decimal Precio,
    int Stock,
    string? ImagenUrl,
    int CategoriaId,
    string CategoriaNombre);

public record ProductoCreateRequest(
    string Nombre,
    string? Descripcion,
    decimal Precio,
    int CategoriaId);

public record ProductoUpdateRequest(
    string Nombre,
    string? Descripcion,
    decimal Precio,
    int CategoriaId);

public record MovimientoItem(
    int Id,
    string Tipo,
    DateTime Fecha,
    int Cantidad,
    decimal PrecioUnitario,
    string Contraparte,
    string? Observaciones);