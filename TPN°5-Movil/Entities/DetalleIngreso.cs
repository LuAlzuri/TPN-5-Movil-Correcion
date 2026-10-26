namespace TPN_5_MOVIL.Entities;

public class DetalleIngreso
{
    public int Id { get; set; }
    public int IngresoId { get; set; }
    public int ProductoId { get; set; }
    public int Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal DescAplicado { get; set; }
    public Ingreso Ingreso { get; set; } = null!;
    public Producto Producto { get; set; } = null!;
}
