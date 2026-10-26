using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

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

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public class ProductoUpdateRequest : IValidatableObject
{
    [Required, MaxLength(150)]
    public string Nombre { get; init; } = string.Empty;
    [MaxLength(1000)]
    public string? Descripcion { get; init; }
    [Required]
    public decimal? Precio { get; init; }
    [Required, Range(1, int.MaxValue)]
    public int? CategoriaId { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Precio is decimal precio && (precio < 0 || precio > 9999999999999999.99m
            || decimal.Round(precio, 2) != precio))
            yield return new ValidationResult("El precio debe ser no negativo, con hasta dos decimales y compatible con decimal(18,2).", [nameof(Precio)]);
    }
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class ProductoCreateRequest : ProductoUpdateRequest { }

public record ImagenResponse(int Id, int ProductoId, string NombreArchivo, string ImagenUrl,
    string TipoContenido, long TamanoBytes);

public sealed class ImagenUploadRequest
{
    [Required]
    public IFormFile? File { get; init; }
}

public record MovimientoItem(
    int Id,
    string Tipo,
    DateTime Fecha,
    int Cantidad,
    decimal PrecioUnitario,
    string Contraparte,
    string? Observaciones);
