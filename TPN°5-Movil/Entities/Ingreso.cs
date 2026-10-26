namespace TPN_5_MOVIL.Entities;

public class Ingreso
{
    public int Id { get; set; }
    public DateTime Fecha { get; set; } = DateTime.UtcNow;
    public string? Observaciones { get; set; }
    public int ProveedorId { get; set; }
    public int EmpleadoId { get; set; }
    public bool Confirmado { get; set; }
    public DateTime? FechaConfirmacion { get; set; }
    public int? EmpleadoConfirmacionId { get; set; }
    public bool Eliminado { get; set; }
    public Proveedor Proveedor { get; set; } = null!;
    public Empleado Empleado { get; set; } = null!;
    public Empleado? EmpleadoConfirmacion { get; set; }
    public List<DetalleIngreso> Detalles { get; set; } = new();
}
