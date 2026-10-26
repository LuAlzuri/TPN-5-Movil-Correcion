namespace TPN_5_MOVIL.Entities;

public class Empleado
{
    public int Id { get; set; }
    public DateOnly FechaIngreso { get; set; }
    public Usuario Usuario { get; set; } = null!;
    public List<Ingreso> Ingresos { get; set; } = new();
    public List<Ingreso> IngresosConfirmados { get; set; } = new();
    public List<Salida> SalidasConfirmadas { get; set; } = new();
}
