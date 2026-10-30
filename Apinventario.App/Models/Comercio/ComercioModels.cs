using System.ComponentModel.DataAnnotations;

namespace Apinventario.App.Models.Comercio;

public sealed record ProductoDto(
    int Id,
    string Nombre,
    string? Descripcion,
    decimal Precio,
    int Stock,
    string? ImagenUrl,
    int CategoriaId,
    string CategoriaNombre);

public sealed record CategoriaCatalogoDto(int Id, string Nombre);

public sealed record ImagenProductoDto(
    int Id,
    int ProductoId,
    string NombreArchivo,
    string ImagenUrl,
    string TipoContenido,
    long TamanoBytes);

public sealed record MovimientoDto(
    int Id,
    string Tipo,
    DateTime Fecha,
    int Cantidad,
    decimal PrecioUnitario,
    string Contraparte,
    string? Observaciones);

public sealed record ProveedorDto(
    int Id,
    string Nombre,
    string CUIT,
    string? Email,
    string? Telefono,
    string? Direccion);

public sealed record ClienteDto(
    int Id,
    string Nombre,
    string Apellido,
    string CUIT,
    string Email,
    string? Telefono,
    string? Direccion);

public sealed record DetalleSalidaDto(
    int Id,
    int ProductoId,
    int Cantidad,
    decimal PrecioUnitario,
    decimal DescAplicado,
    decimal Subtotal);

public sealed record SalidaDto(
    int Id,
    int ClienteId,
    int? UsuarioConfirmacionId,
    DateTime Fecha,
    string? Observaciones,
    bool ConfirmarXCliente,
    bool ConfirmarXEmpleado,
    DateTime? FechaConfirmacionCliente,
    DateTime? FechaConfirmacionEmpleado,
    List<DetalleSalidaDto> Detalles,
    decimal Total,
    bool Eliminado,
    string? MedioPago = null);

public sealed record DetalleIngresoDto(
    int Id,
    int ProductoId,
    int Cantidad,
    decimal PrecioUnitario,
    decimal DescAplicado,
    decimal Subtotal);

public sealed record IngresoDto(
    int Id,
    int ProveedorId,
    int EmpleadoId,
    DateTime Fecha,
    string? Observaciones,
    bool Confirmado,
    DateTime? FechaConfirmacion,
    int? EmpleadoConfirmacionId,
    List<DetalleIngresoDto> Detalles,
    decimal Total);

public sealed class ProductoForm : IValidatableObject
{
    [Required, MaxLength(150)]
    public string Nombre { get; set; } = "";
    [MaxLength(1000)]
    public string? Descripcion { get; set; }
    [Required, Range(0, 9999999999999999.99)]
    public decimal? Precio { get; set; }
    [Required, Range(1, int.MaxValue)]
    public int? CategoriaId { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Precio is decimal precio && decimal.Round(precio, 2) != precio)
            yield return new("El precio debe tener hasta dos decimales.", [nameof(Precio)]);
    }
}

public sealed class ProveedorForm
{
    [Required, MaxLength(150)]
    public string Nombre { get; set; } = "";
    [Required, MaxLength(20)]
    public string CUIT { get; set; } = "";
    [EmailAddress, MaxLength(150)]
    public string? Email { get; set; }
    [MaxLength(50)]
    public string? Telefono { get; set; }
    [MaxLength(300)]
    public string? Direccion { get; set; }
}

public sealed class IngresoForm
{
    [Required, Range(1, int.MaxValue)]
    public int? ProveedorId { get; set; }
    [MaxLength(1000)]
    public string? Observaciones { get; set; }
}

public sealed class DetalleIngresoForm : IValidatableObject
{
    [Required, Range(1, int.MaxValue)]
    public int? ProductoId { get; set; }
    [Required, Range(1, int.MaxValue)]
    public int? Cantidad { get; set; }
    [Range(0, 100)]
    public decimal DescAplicado { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (decimal.Round(DescAplicado, 2) != DescAplicado)
            yield return new("El descuento debe tener hasta dos decimales.", [nameof(DescAplicado)]);
    }
}

public sealed class CantidadSalidaForm
{
    [Required, Range(0, int.MaxValue)]
    public int? Cantidad { get; set; }
}

public static class PedidoEstado
{
    public const string Borrador = "Borrador";
    public const string Enviado = "Enviado, pendiente de aceptación";
    public const string Aceptado = "Aceptado";
    public const string Cancelado = "Cancelado";

    public static string De(SalidaDto salida) => salida.Eliminado ? Cancelado
        : salida.ConfirmarXEmpleado ? Aceptado
        : salida.ConfirmarXCliente ? Enviado
        : Borrador;

    public static bool EsBorradorActivo(SalidaDto salida) =>
        !salida.Eliminado && !salida.ConfirmarXCliente && !salida.ConfirmarXEmpleado;
}

public static class ComercioDates
{
    public static string? Start(DateTime? date) => date.HasValue
        ? new DateTimeOffset(date.Value.Date, TimeSpan.FromHours(-3)).UtcDateTime.ToString("O")
        : null;

    public static string? End(DateTime? date) => date.HasValue && date.Value.Date < DateTime.MaxValue.Date
        ? new DateTimeOffset(date.Value.Date.AddDays(1).AddTicks(-1), TimeSpan.FromHours(-3)).UtcDateTime.ToString("O")
        : null;
}
