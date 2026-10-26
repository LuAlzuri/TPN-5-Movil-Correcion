namespace TPN_5_MOVIL.Entities;

public class DetalleSalida
{
    public int Id { get; set; }
    public int SalidaId { get; set; }
    public int ProductoId { get; set; }
    public int Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal DescAplicado { get; set; }
    public Salida Salida { get; set; } = null!;
    public Producto Producto { get; set; } = null!;
}
