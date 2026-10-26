namespace TPN_5_MOVIL.Entities;

public class Cliente
{
    public int Id { get; set; }
    public string? Telefono { get; set; }
    public string? Direccion { get; set; }
    public Usuario Usuario { get; set; } = null!;
    public List<Salida> Salidas { get; set; } = new();
}
