namespace TPN_5_MOVIL.Entities;

public class Producto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public decimal Precio { get; set; }
    public int Stock { get; set; }
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public bool Eliminado { get; set; }
    public int CategoriaId { get; set; }
    public Categoria Categoria { get; set; } = null!;
    public Imagen? Imagen { get; set; }
    public List<DetalleIngreso> DetallesIngreso { get; set; } = new();
    public List<DetalleSalida> DetallesSalida { get; set; } = new();
}
