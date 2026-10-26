namespace TPN_5_MOVIL.Entities;

public class Imagen
{
    public int Id { get; set; }
    public int ProductoId { get; set; }
    public string NombreArchivo { get; set; } = string.Empty;
    public string RutaRelativa { get; set; } = string.Empty;
    public string TipoContenido { get; set; } = string.Empty;
    public long TamanoBytes { get; set; }
    public Producto Producto { get; set; } = null!;
}
