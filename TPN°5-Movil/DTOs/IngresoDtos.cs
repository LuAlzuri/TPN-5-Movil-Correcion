using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace TPN_5_MOVIL.DTOs;

public record DetalleIngresoResponse(int Id, int ProductoId, int Cantidad, decimal PrecioUnitario, decimal DescAplicado, decimal Subtotal);
public record IngresoResponse(int Id, int ProveedorId, int EmpleadoId, DateTime Fecha, string? Observaciones,
    bool Confirmado, DateTime? FechaConfirmacion, int? EmpleadoConfirmacionId, List<DetalleIngresoResponse> Detalles, decimal Total);

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class IngresoRequest
{
    [Required, Range(1, int.MaxValue)]
    public int? ProveedorId { get; init; }
    [MaxLength(1000)]
    public string? Observaciones { get; init; }
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public class DetalleIngresoUpdateRequest : IValidatableObject
{
    [Required, Range(1, int.MaxValue)]
    public int? Cantidad { get; init; }
    public decimal DescAplicado { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (DescAplicado < 0 || DescAplicado > 100 || decimal.Round(DescAplicado, 2) != DescAplicado)
            yield return new ValidationResult("El descuento debe estar entre 0 y 100, con hasta dos decimales.", [nameof(DescAplicado)]);
    }
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class DetalleIngresoCreateRequest : DetalleIngresoUpdateRequest
{
    [Required, Range(1, int.MaxValue)]
    public int? ProductoId { get; init; }
}
