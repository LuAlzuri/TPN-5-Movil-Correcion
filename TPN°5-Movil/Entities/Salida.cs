namespace TPN_5_MOVIL.Entities;

public class Salida
{
    public int Id { get; set; }
    public DateTime Fecha { get; set; } = DateTime.UtcNow;
    public string? Observaciones { get; set; }
    public int ClienteId { get; set; }
    public int? UsuarioConfirmacionId { get; set; }
    public bool ConfirmarXCliente { get; set; }
    public bool ConfirmarXEmpleado { get; set; }
    public DateTime? FechaConfirmacionCliente { get; set; }
    public DateTime? FechaConfirmacionEmpleado { get; set; }
    public bool Eliminado { get; set; }
    public Cliente Cliente { get; set; } = null!;
    public Usuario? UsuarioConfirmacion { get; set; }
    public List<DetalleSalida> Detalles { get; set; } = new();
}
