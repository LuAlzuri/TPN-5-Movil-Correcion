using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace TPN_5_MOVIL.DTOs;

public record DetalleSalidaResponse(int Id, int ProductoId, int Cantidad, decimal PrecioUnitario, decimal DescAplicado, decimal Subtotal);
public record SalidaResponse(int Id, int ClienteId, int? UsuarioConfirmacionId, DateTime Fecha, string? Observaciones,
    bool ConfirmarXCliente, bool ConfirmarXEmpleado, DateTime? FechaConfirmacionCliente,
    DateTime? FechaConfirmacionEmpleado, List<DetalleSalidaResponse> Detalles, decimal Total)
{
    public bool Eliminado { get; init; }
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class SalidaRequest
{
    [MaxLength(1000)]
    public string? Observaciones { get; init; }
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public class DetalleSalidaUpdateRequest
{
    [Required, Range(0, int.MaxValue)]
    public int? Cantidad { get; init; }
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class DetalleSalidaCreateRequest : DetalleSalidaUpdateRequest
{
    [Required, Range(1, int.MaxValue)]
    public int? ProductoId { get; init; }
}
