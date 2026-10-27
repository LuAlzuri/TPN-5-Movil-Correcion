using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using TPN_5_MOVIL.Data;
using TPN_5_MOVIL.DTOs;
using TPN_5_MOVIL.Entities;
using TPN_5_MOVIL.Exceptions;

namespace TPN_5_MOVIL.Services;

// This service is exposed only by the Administrator-only, read-only audit controller.
public sealed class AuditoriaService(AppDbContext db)
{
    private IQueryable<T> Historico<T>() where T : class => db.Set<T>().IgnoreQueryFilters().AsNoTracking();
    private static string? Texto(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private IQueryable<AuditoriaUsuarioResponse> Usuarios(IQueryable<Usuario> query) => query.Select(u =>
        new AuditoriaUsuarioResponse(u.Id, u.Nombre, u.Apellido, u.CUIT, u.Email, u.Rol, u.Eliminado, u.Empleado != null, u.Cliente != null));

    public Task<PaginatedResult<AuditoriaUsuarioResponse>> UsuariosAsync(AuditoriaUsuariosFiltro f, CancellationToken ct)
    {
        var q = Historico<Usuario>();
        if (f.Eliminado.HasValue) q = q.Where(u => u.Eliminado == f.Eliminado.Value);
        if (Texto(f.Rol) is string role) q = q.Where(u => u.Rol == role);
        if (Texto(f.Busqueda) is string text) q = q.Where(u => (u.Nombre + " " + u.Apellido).Contains(text) || u.Email.Contains(text) || u.CUIT.Contains(text));
        return PaginaAsync(Usuarios(q.OrderBy(u => u.Email).ThenBy(u => u.Id)), f, ct);
    }
    public Task<AuditoriaUsuarioResponse> UsuarioAsync(int id, CancellationToken ct) =>
        UnoAsync(Usuarios(Historico<Usuario>().Where(u => u.Id == id)), ct);

    private IQueryable<AuditoriaEmpleadoResponse> Empleados(IQueryable<Empleado> query) => query.Select(e =>
        new AuditoriaEmpleadoResponse(e.Id, e.Usuario.Eliminado,
            new AuditoriaUsuarioResponse(e.Id, e.Usuario.Nombre, e.Usuario.Apellido, e.Usuario.CUIT, e.Usuario.Email,
                e.Usuario.Rol, e.Usuario.Eliminado, true, e.Usuario.Cliente != null), e.FechaIngreso,
            e.Usuario.Rol == Usuario.RolEmpleado && e.Usuario.Cliente == null));
    public Task<PaginatedResult<AuditoriaEmpleadoResponse>> EmpleadosAsync(AuditoriaBusquedaFiltro f, CancellationToken ct)
    {
        var q = Historico<Empleado>();
        if (f.Eliminado.HasValue) q = q.Where(e => e.Usuario.Eliminado == f.Eliminado.Value);
        if (Texto(f.Busqueda) is string text) q = q.Where(e => (e.Usuario.Nombre + " " + e.Usuario.Apellido).Contains(text) || e.Usuario.Email.Contains(text) || e.Usuario.CUIT.Contains(text));
        return PaginaAsync(Empleados(q.OrderBy(e => e.Usuario.Apellido).ThenBy(e => e.Usuario.Nombre).ThenBy(e => e.Id)), f, ct);
    }
    public Task<AuditoriaEmpleadoResponse> EmpleadoAsync(int id, CancellationToken ct) =>
        UnoAsync(Empleados(Historico<Empleado>().Where(e => e.Id == id)), ct);

    private IQueryable<AuditoriaClienteResponse> Clientes(IQueryable<Cliente> query) => query.Select(c =>
        new AuditoriaClienteResponse(c.Id, c.Usuario.Eliminado,
            new AuditoriaUsuarioResponse(c.Id, c.Usuario.Nombre, c.Usuario.Apellido, c.Usuario.CUIT, c.Usuario.Email,
                c.Usuario.Rol, c.Usuario.Eliminado, c.Usuario.Empleado != null, true), c.Telefono, c.Direccion,
            c.Usuario.Rol == Usuario.RolCliente && c.Usuario.Empleado == null));
    public Task<PaginatedResult<AuditoriaClienteResponse>> ClientesAsync(AuditoriaBusquedaFiltro f, CancellationToken ct)
    {
        var q = Historico<Cliente>();
        if (f.Eliminado.HasValue) q = q.Where(c => c.Usuario.Eliminado == f.Eliminado.Value);
        if (Texto(f.Busqueda) is string text) q = q.Where(c => (c.Usuario.Nombre + " " + c.Usuario.Apellido).Contains(text) || c.Usuario.Email.Contains(text) || c.Usuario.CUIT.Contains(text));
        return PaginaAsync(Clientes(q.OrderBy(c => c.Usuario.Apellido).ThenBy(c => c.Usuario.Nombre).ThenBy(c => c.Id)), f, ct);
    }
    public Task<AuditoriaClienteResponse> ClienteAsync(int id, CancellationToken ct) =>
        UnoAsync(Clientes(Historico<Cliente>().Where(c => c.Id == id)), ct);

    private static IQueryable<AuditoriaCategoriaResponse> Categorias(IQueryable<Categoria> query) => query.Select(c =>
        new AuditoriaCategoriaResponse(c.Id, c.Nombre, c.Descripcion, c.FechaCreacion, c.Eliminado));
    public Task<PaginatedResult<AuditoriaCategoriaResponse>> CategoriasAsync(AuditoriaBusquedaFiltro f, CancellationToken ct)
    {
        var q = Historico<Categoria>();
        if (f.Eliminado.HasValue) q = q.Where(c => c.Eliminado == f.Eliminado.Value);
        if (Texto(f.Busqueda) is string text) q = q.Where(c => c.Nombre.Contains(text) || (c.Descripcion != null && c.Descripcion.Contains(text)));
        return PaginaAsync(Categorias(q.OrderBy(c => c.Nombre).ThenBy(c => c.Id)), f, ct);
    }
    public Task<AuditoriaCategoriaResponse> CategoriaAsync(int id, CancellationToken ct) =>
        UnoAsync(Categorias(Historico<Categoria>().Where(c => c.Id == id)), ct);

    private static IQueryable<AuditoriaProveedorResponse> Proveedores(IQueryable<Proveedor> query) => query.Select(p =>
        new AuditoriaProveedorResponse(p.Id, p.Nombre, p.CUIT, p.Email, p.Telefono, p.Direccion, p.Eliminado));
    public Task<PaginatedResult<AuditoriaProveedorResponse>> ProveedoresAsync(AuditoriaBusquedaFiltro f, CancellationToken ct)
    {
        var q = Historico<Proveedor>();
        if (f.Eliminado.HasValue) q = q.Where(p => p.Eliminado == f.Eliminado.Value);
        if (Texto(f.Busqueda) is string text) q = q.Where(p => p.Nombre.Contains(text) || p.CUIT.Contains(text) || (p.Email != null && p.Email.Contains(text)));
        return PaginaAsync(Proveedores(q.OrderBy(p => p.Nombre).ThenBy(p => p.Id)), f, ct);
    }
    public Task<AuditoriaProveedorResponse> ProveedorAsync(int id, CancellationToken ct) =>
        UnoAsync(Proveedores(Historico<Proveedor>().Where(p => p.Id == id)), ct);

    private static IQueryable<AuditoriaProductoResponse> Productos(IQueryable<Producto> query) => query.Select(p =>
        new AuditoriaProductoResponse(p.Id, p.Nombre, p.Descripcion, p.Precio, p.Stock, p.FechaCreacion, p.Eliminado,
            new AuditoriaReferenciaResponse(p.CategoriaId, p.Categoria.Nombre, p.Categoria.Eliminado),
            p.Imagen == null ? null : new ImagenResponse(p.Imagen.Id, p.Id, p.Imagen.NombreArchivo,
                p.Imagen.RutaRelativa, p.Imagen.TipoContenido, p.Imagen.TamanoBytes)));
    public Task<PaginatedResult<AuditoriaProductoResponse>> ProductosAsync(AuditoriaProductosFiltro f, CancellationToken ct)
    {
        var q = Historico<Producto>();
        if (f.Eliminado.HasValue) q = q.Where(p => p.Eliminado == f.Eliminado.Value);
        if (f.CategoriaId.HasValue) q = q.Where(p => p.CategoriaId == f.CategoriaId.Value);
        if (Texto(f.Busqueda) is string text) q = q.Where(p => p.Nombre.Contains(text) || (p.Descripcion != null && p.Descripcion.Contains(text)));
        return PaginaAsync(Productos(q.OrderBy(p => p.Nombre).ThenBy(p => p.Id)), f, ct);
    }
    public Task<AuditoriaProductoResponse> ProductoAsync(int id, CancellationToken ct) =>
        UnoAsync(Productos(Historico<Producto>().Where(p => p.Id == id)), ct);

    private static IQueryable<AuditoriaIngresoResumen> Ingresos(IQueryable<Ingreso> query) => query.Select(i =>
        new AuditoriaIngresoResumen(i.Id, i.Fecha, i.Observaciones, i.Eliminado,
            new AuditoriaReferenciaResponse(i.ProveedorId, i.Proveedor.Nombre, i.Proveedor.Eliminado),
            new AuditoriaUsuarioReferencia(i.EmpleadoId, i.Empleado.Usuario.Nombre, i.Empleado.Usuario.Apellido, i.Empleado.Usuario.Rol, i.Empleado.Usuario.Eliminado),
            i.Confirmado, i.FechaConfirmacion,
            i.EmpleadoConfirmacion == null ? null : new AuditoriaUsuarioReferencia(i.EmpleadoConfirmacion.Id,
                i.EmpleadoConfirmacion.Usuario.Nombre, i.EmpleadoConfirmacion.Usuario.Apellido, i.EmpleadoConfirmacion.Usuario.Rol, i.EmpleadoConfirmacion.Usuario.Eliminado),
            i.Eliminado && i.Confirmado));
    public Task<PaginatedResult<AuditoriaIngresoResumen>> IngresosAsync(AuditoriaIngresosFiltro f, CancellationToken ct)
    {
        var q = Historico<Ingreso>();
        if (f.Eliminado.HasValue) q = q.Where(i => i.Eliminado == f.Eliminado.Value);
        if (f.ProveedorId.HasValue) q = q.Where(i => i.ProveedorId == f.ProveedorId.Value);
        if (f.ProductoId.HasValue) q = q.Where(i => i.Detalles.Any(d => d.ProductoId == f.ProductoId.Value));
        if (f.Confirmado.HasValue) q = q.Where(i => i.Confirmado == f.Confirmado.Value);
        if (f.Desde.HasValue) q = q.Where(i => i.Fecha >= f.Desde.Value);
        if (f.Hasta.HasValue) q = q.Where(i => i.Fecha <= f.Hasta.Value);
        return PaginaAsync(Ingresos(q.OrderByDescending(i => i.Fecha).ThenByDescending(i => i.Id)), f, ct);
    }
    public async Task<AuditoriaIngresoResponse> IngresoAsync(int id, CancellationToken ct)
    {
        var header = await UnoAsync(Ingresos(Historico<Ingreso>().Where(i => i.Id == id)), ct);
        var details = await Historico<DetalleIngreso>().Where(d => d.IngresoId == id).OrderBy(d => d.Id).Select(d =>
            new AuditoriaDetalleResponse(d.Id, new AuditoriaReferenciaResponse(d.ProductoId, d.Producto.Nombre, d.Producto.Eliminado),
                d.Cantidad, d.PrecioUnitario, d.DescAplicado, ImportesMovimiento.Subtotal(d.Cantidad, d.PrecioUnitario, d.DescAplicado))).ToListAsync(ct);
        return new(header, details, details.Sum(d => d.Subtotal));
    }

    private static IQueryable<AuditoriaSalidaResumen> Salidas(IQueryable<Salida> query) => query.Select(s =>
        new AuditoriaSalidaResumen(s.Id, s.Fecha, s.Observaciones, s.Eliminado,
            new AuditoriaUsuarioReferencia(s.ClienteId, s.Cliente.Usuario.Nombre, s.Cliente.Usuario.Apellido, s.Cliente.Usuario.Rol, s.Cliente.Usuario.Eliminado),
            s.ConfirmarXCliente, s.ConfirmarXEmpleado, s.FechaConfirmacionCliente, s.FechaConfirmacionEmpleado,
            s.UsuarioConfirmacion == null ? null : new AuditoriaUsuarioReferencia(s.UsuarioConfirmacion.Id,
                s.UsuarioConfirmacion.Nombre, s.UsuarioConfirmacion.Apellido, s.UsuarioConfirmacion.Rol, s.UsuarioConfirmacion.Eliminado)));
    public Task<PaginatedResult<AuditoriaSalidaResumen>> SalidasAsync(AuditoriaSalidasFiltro f, CancellationToken ct)
    {
        var q = Historico<Salida>();
        if (f.Eliminado.HasValue) q = q.Where(s => s.Eliminado == f.Eliminado.Value);
        if (f.ClienteId.HasValue) q = q.Where(s => s.ClienteId == f.ClienteId.Value);
        if (f.ProductoId.HasValue) q = q.Where(s => s.Detalles.Any(d => d.ProductoId == f.ProductoId.Value));
        if (f.ConfirmarXCliente.HasValue) q = q.Where(s => s.ConfirmarXCliente == f.ConfirmarXCliente.Value);
        if (f.ConfirmarXEmpleado.HasValue) q = q.Where(s => s.ConfirmarXEmpleado == f.ConfirmarXEmpleado.Value);
        if (f.Desde.HasValue) q = q.Where(s => s.Fecha >= f.Desde.Value);
        if (f.Hasta.HasValue) q = q.Where(s => s.Fecha <= f.Hasta.Value);
        return PaginaAsync(Salidas(q.OrderByDescending(s => s.Fecha).ThenByDescending(s => s.Id)), f, ct);
    }
    public async Task<AuditoriaSalidaResponse> SalidaAsync(int id, CancellationToken ct)
    {
        var header = await UnoAsync(Salidas(Historico<Salida>().Where(s => s.Id == id)), ct);
        var details = await Historico<DetalleSalida>().Where(d => d.SalidaId == id).OrderBy(d => d.Id).Select(d =>
            new AuditoriaDetalleResponse(d.Id, new AuditoriaReferenciaResponse(d.ProductoId, d.Producto.Nombre, d.Producto.Eliminado),
                d.Cantidad, d.PrecioUnitario, d.DescAplicado, ImportesMovimiento.Subtotal(d.Cantidad, d.PrecioUnitario, d.DescAplicado))).ToListAsync(ct);
        return new(header, details, details.Sum(d => d.Subtotal));
    }

    public async Task<PaginatedResult<AuditoriaMovimientoResponse>> MovimientosAsync(int productoId, AuditoriaMovimientosFiltro f, CancellationToken ct)
    {
        Validar(f);
        if (!await Historico<Producto>().AnyAsync(p => p.Id == productoId, ct)) throw NoEncontrado();
        // Flat projections keep filtering, UNION ALL, ordering and pagination in SQL.
        var incoming = Historico<DetalleIngreso>().Where(d => d.ProductoId == productoId).Select(d => new MovimientoFila
        {
            EncabezadoId = d.IngresoId, DetalleId = d.Id, ProductoId = d.ProductoId, Tipo = "Ingreso",
            Fecha = d.Ingreso.Fecha, FechaConfirmacion = d.Ingreso.FechaConfirmacion,
            Eliminado = d.Ingreso.Eliminado, Confirmado = d.Ingreso.Confirmado, Solicitado = false,
            Cantidad = d.Cantidad, PrecioUnitario = d.PrecioUnitario, Descuento = d.DescAplicado,
            ContraparteId = d.Ingreso.ProveedorId, ContraparteNombre = d.Ingreso.Proveedor.Nombre, ContraparteEliminado = d.Ingreso.Proveedor.Eliminado
        });
        var outgoing = Historico<DetalleSalida>().Where(d => d.ProductoId == productoId).Select(d => new MovimientoFila
        {
            EncabezadoId = d.SalidaId, DetalleId = d.Id, ProductoId = d.ProductoId, Tipo = "Salida",
            Fecha = d.Salida.Fecha, FechaConfirmacion = d.Salida.FechaConfirmacionEmpleado,
            Eliminado = d.Salida.Eliminado, Confirmado = d.Salida.ConfirmarXEmpleado, Solicitado = d.Salida.ConfirmarXCliente,
            Cantidad = -d.Cantidad, PrecioUnitario = d.PrecioUnitario, Descuento = d.DescAplicado,
            ContraparteId = d.Salida.ClienteId, ContraparteNombre = d.Salida.Cliente.Usuario.Nombre + " " + d.Salida.Cliente.Usuario.Apellido,
            ContraparteEliminado = d.Salida.Cliente.Usuario.Eliminado
        });
        var query = incoming.Concat(outgoing);
        if (f.Eliminado.HasValue) query = query.Where(m => m.Eliminado == f.Eliminado.Value);
        if (Texto(f.Tipo) is string type) query = query.Where(m => m.Tipo == type);
        if (f.Desde.HasValue) query = query.Where(m => m.Fecha >= f.Desde.Value);
        if (f.Hasta.HasValue) query = query.Where(m => m.Fecha <= f.Hasta.Value);
        var page = await PaginaAsync(query.OrderByDescending(m => m.Fecha).ThenBy(m => m.Tipo)
            .ThenBy(m => m.EncabezadoId).ThenBy(m => m.DetalleId), f, ct);
        return new()
        {
            Page = page.Page, PageSize = page.PageSize, TotalCount = page.TotalCount, TotalPages = page.TotalPages,
            HasNext = page.HasNext, HasPrevious = page.HasPrevious,
            Items = page.Items.Select(m => new AuditoriaMovimientoResponse(m.EncabezadoId, m.DetalleId, m.ProductoId, m.Tipo,
                m.Fecha, m.FechaConfirmacion, m.Eliminado, Estado(m), m.Cantidad,
                m.Confirmado && !m.Eliminado ? m.Cantidad : 0, m.PrecioUnitario, m.Descuento,
                ImportesMovimiento.Subtotal(Math.Abs(m.Cantidad), m.PrecioUnitario, m.Descuento),
                new AuditoriaReferenciaResponse(m.ContraparteId, m.ContraparteNombre, m.ContraparteEliminado))).ToList()
        };
    }

    private static string Estado(MovimientoFila m) => m.Tipo == "Ingreso"
        ? m.Eliminado ? m.Confirmado ? "Revertido" : "BorradorEliminado" : m.Confirmado ? "Confirmado" : "Borrador"
        : m.Eliminado ? "Eliminada" : m.Confirmado ? "Confirmada" : m.Solicitado ? "Solicitada" : "Borrador";
    private static ApiException NoEncontrado() => new("El registro no existe.", StatusCodes.Status404NotFound);
    private static async Task<T> UnoAsync<T>(IQueryable<T> query, CancellationToken ct) where T : class =>
        await query.SingleOrDefaultAsync(ct) ?? throw NoEncontrado();
    private static void Validar(AuditoriaPaginaFiltro f)
    {
        var errors = new List<ValidationResult>();
        if (!Validator.TryValidateObject(f, new ValidationContext(f), errors, true))
            throw new ApiException("Filtros de auditoría inválidos: " + string.Join(" ", errors.Select(e => e.ErrorMessage)));
    }
    private static async Task<PaginatedResult<T>> PaginaAsync<T>(IQueryable<T> query, AuditoriaPaginaFiltro f, CancellationToken ct)
    {
        Validar(f);
        var count = await query.CountAsync(ct);
        return PaginatedResult<T>.From(
            await query.Skip((f.Page - 1) * f.PageSize).Take(f.PageSize).ToListAsync(ct),
            count, f.Page, f.PageSize);
    }
    private sealed class MovimientoFila
    {
        public int EncabezadoId { get; init; }
        public int DetalleId { get; init; }
        public int ProductoId { get; init; }
        public string Tipo { get; init; } = "";
        public DateTime Fecha { get; init; }
        public DateTime? FechaConfirmacion { get; init; }
        public bool Eliminado { get; init; }
        public bool Confirmado { get; init; }
        public bool Solicitado { get; init; }
        public int Cantidad { get; init; }
        public decimal PrecioUnitario { get; init; }
        public decimal Descuento { get; init; }
        public int ContraparteId { get; init; }
        public string ContraparteNombre { get; init; } = "";
        public bool ContraparteEliminado { get; init; }
    }
}
