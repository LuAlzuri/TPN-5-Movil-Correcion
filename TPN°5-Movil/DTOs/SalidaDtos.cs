namespace TPN_5_MOVIL.DTOs;

public record DetalleSalidaResponse(int Id, int ProductoId, int Cantidad, decimal PrecioUnitario, decimal DescAplicado);
public record SalidaResponse(int Id, int ClienteId, int? EmpleadoId, DateTime Fecha, string? Observaciones,
    bool ConfirmarXCliente, bool ConfirmarXEmpleado, DateTime? FechaConfirmacionCliente,
    DateTime? FechaConfirmacionEmpleado, List<DetalleSalidaResponse> Detalles);
public record SalidaRequest(string? Observaciones);
