using System.ComponentModel.DataAnnotations;
using TPN_5_MOVIL.Entities;

namespace TPN_5_MOVIL.DTOs;

public class AuditoriaPaginaFiltro : IValidatableObject
{
    [Range(1, int.MaxValue)] public int Page { get; init; } = 1;
    [Range(1, 100)] public int PageSize { get; init; } = 10;
    public bool? Eliminado { get; init; }

    public virtual IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if ((long)(Page - 1) * PageSize > int.MaxValue)
            yield return new ValidationResult("El desplazamiento de página excede el rango permitido.", [nameof(Page)]);
    }
}

public class AuditoriaBusquedaFiltro : AuditoriaPaginaFiltro
{
    [MaxLength(300)] public string? Busqueda { get; init; }
}

public sealed class AuditoriaUsuariosFiltro : AuditoriaBusquedaFiltro
{
    public string? Rol { get; init; }
    public override IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        foreach (var error in base.Validate(context)) yield return error;
        if (!string.IsNullOrWhiteSpace(Rol) && Rol.Trim() is not (Usuario.RolAdministrador or Usuario.RolEmpleado or Usuario.RolCliente))
            yield return new ValidationResult("Rol inválido.", [nameof(Rol)]);
    }
}

public sealed class AuditoriaProductosFiltro : AuditoriaBusquedaFiltro
{
    [Range(1, int.MaxValue)] public int? CategoriaId { get; init; }
}

public class AuditoriaFechasFiltro : AuditoriaPaginaFiltro
{
    public DateTime? Desde { get; init; }
    public DateTime? Hasta { get; init; }
    public override IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        foreach (var error in base.Validate(context)) yield return error;
        if (Desde > Hasta) yield return new ValidationResult("Desde no puede ser posterior a Hasta.", [nameof(Desde), nameof(Hasta)]);
    }
}

public sealed class AuditoriaIngresosFiltro : AuditoriaFechasFiltro
{
    [Range(1, int.MaxValue)] public int? ProveedorId { get; init; }
    [Range(1, int.MaxValue)] public int? ProductoId { get; init; }
    public bool? Confirmado { get; init; }
}

public sealed class AuditoriaSalidasFiltro : AuditoriaFechasFiltro
{
    [Range(1, int.MaxValue)] public int? ClienteId { get; init; }
    [Range(1, int.MaxValue)] public int? ProductoId { get; init; }
    public bool? ConfirmarXCliente { get; init; }
    public bool? ConfirmarXEmpleado { get; init; }
}

public sealed class AuditoriaMovimientosFiltro : AuditoriaFechasFiltro
{
    public string? Tipo { get; init; }
    public override IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        foreach (var error in base.Validate(context)) yield return error;
        if (!string.IsNullOrWhiteSpace(Tipo) && Tipo.Trim() is not ("Ingreso" or "Salida"))
            yield return new ValidationResult("Tipo debe ser Ingreso o Salida.", [nameof(Tipo)]);
    }
}

public record AuditoriaUsuarioResponse(int Id, string Nombre, string Apellido, string CUIT, string Email,
    string Rol, bool Eliminado, bool TienePerfilEmpleado, bool TienePerfilCliente);
public record AuditoriaEmpleadoResponse(int Id, bool Eliminado, AuditoriaUsuarioResponse Usuario, DateOnly FechaIngreso, bool PerfilConsistente);
public record AuditoriaClienteResponse(int Id, bool Eliminado, AuditoriaUsuarioResponse Usuario, string? Telefono, string? Direccion, bool PerfilConsistente);
public record AuditoriaReferenciaResponse(int Id, string Nombre, bool Eliminado);
public record AuditoriaUsuarioReferencia(int Id, string Nombre, string Apellido, string Rol, bool Eliminado);
public record AuditoriaCategoriaResponse(int Id, string Nombre, string? Descripcion, DateTime FechaCreacion, bool Eliminado);
public record AuditoriaProveedorResponse(int Id, string Nombre, string CUIT, string? Email, string? Telefono, string? Direccion, bool Eliminado);
public record AuditoriaProductoResponse(int Id, string Nombre, string? Descripcion, decimal Precio, int Stock,
    DateTime FechaCreacion, bool Eliminado, AuditoriaReferenciaResponse Categoria, ImagenResponse? Imagen);
public record AuditoriaDetalleResponse(int Id, AuditoriaReferenciaResponse Producto, int Cantidad, decimal PrecioUnitario, decimal DescAplicado, decimal Subtotal);
public record AuditoriaIngresoResumen(int Id, DateTime Fecha, string? Observaciones, bool Eliminado,
    AuditoriaReferenciaResponse Proveedor, AuditoriaUsuarioReferencia EmpleadoCreador,
    bool Confirmado, DateTime? FechaConfirmacion, AuditoriaUsuarioReferencia? EmpleadoConfirmacion, bool Revertido);
public record AuditoriaIngresoResponse(AuditoriaIngresoResumen Encabezado, List<AuditoriaDetalleResponse> Detalles, decimal Total);
public record AuditoriaSalidaResumen(int Id, DateTime Fecha, string? Observaciones, bool Eliminado,
    AuditoriaUsuarioReferencia Cliente, bool ConfirmarXCliente, bool ConfirmarXEmpleado,
    DateTime? FechaConfirmacionCliente, DateTime? FechaConfirmacionEmpleado, AuditoriaUsuarioReferencia? UsuarioConfirmacion);
public record AuditoriaSalidaResponse(AuditoriaSalidaResumen Encabezado, List<AuditoriaDetalleResponse> Detalles, decimal Total);
public record AuditoriaMovimientoResponse(int EncabezadoId, int DetalleId, int ProductoId, string Tipo,
    DateTime Fecha, DateTime? FechaConfirmacion, bool Eliminado, string Estado,
    int Cantidad, int ImpactoStockVigente, decimal PrecioUnitario, decimal DescAplicado, decimal Subtotal,
    AuditoriaReferenciaResponse Contraparte);
