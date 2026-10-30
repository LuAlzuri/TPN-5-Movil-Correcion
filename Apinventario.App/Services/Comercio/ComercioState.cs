namespace Apinventario.App.Services.Comercio;

public sealed class TiendaState
{
    public string? Busqueda { get; set; }
    public int? CategoriaId { get; set; }
    public int Page { get; set; } = 1;
}

public sealed class CarritoState
{
    public int? SalidaId { get; set; }
}
