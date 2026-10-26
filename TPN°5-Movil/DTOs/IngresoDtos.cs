namespace TPN_5_MOVIL.DTOs;

public record DetalleIngresoResponse(int Id, int ProductoId, int Cantidad, decimal PrecioUnitario, decimal DescAplicado);
public record IngresoResponse(int Id, int ProveedorId, int EmpleadoId, DateTime Fecha, string? Observaciones,
    bool Confirmado, DateTime? FechaConfirmacion, int? EmpleadoConfirmacionId, List<DetalleIngresoResponse> Detalles);
public record IngresoRequest(int ProveedorId, string? Observaciones);
