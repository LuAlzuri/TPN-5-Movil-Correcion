namespace TPN_5_MOVIL.Entities;

public class Categoria
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public bool Eliminado { get; set; }
    public List<Producto> Productos { get; set; } = new();
}
