using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TPN_5_MOVIL.Data;
using TPN_5_MOVIL.DTOs;
using TPN_5_MOVIL.Entities;
using TPN_5_MOVIL.Services;

namespace TPN_5_MOVIL.Controllers;

[ApiController]
[Route("api/auditoria")]
[Authorize(Roles = Usuario.RolAdministrador)]
public sealed class AuditoriaController(AuditoriaService auditoria, AppDbContext db) : ControllerBase
{
    [HttpGet("usuarios")]
    public async Task<ActionResult<PaginatedResult<AuditoriaUsuarioResponse>>> Usuarios([FromQuery] AuditoriaUsuariosFiltro filtro, CancellationToken ct) =>
        Ok(await auditoria.UsuariosAsync(filtro, ct));
    [HttpGet("usuarios/{id:int}")]
    public async Task<ActionResult<AuditoriaUsuarioResponse>> UsuarioPorId(int id, CancellationToken ct) =>
        Ok(await auditoria.UsuarioAsync(id, ct));

    [HttpGet("empleados")]
    public async Task<ActionResult<PaginatedResult<AuditoriaEmpleadoResponse>>> Empleados([FromQuery] AuditoriaBusquedaFiltro filtro, CancellationToken ct) =>
        Ok(await auditoria.EmpleadosAsync(filtro, ct));
    [HttpGet("empleados/{id:int}")]
    public async Task<ActionResult<AuditoriaEmpleadoResponse>> Empleado(int id, CancellationToken ct) =>
        Ok(await auditoria.EmpleadoAsync(id, ct));

    [HttpGet("clientes")]
    public async Task<ActionResult<PaginatedResult<AuditoriaClienteResponse>>> Clientes([FromQuery] AuditoriaBusquedaFiltro filtro, CancellationToken ct) =>
        Ok(await auditoria.ClientesAsync(filtro, ct));
    [HttpGet("clientes/{id:int}")]
    public async Task<ActionResult<AuditoriaClienteResponse>> Cliente(int id, CancellationToken ct) =>
        Ok(await auditoria.ClienteAsync(id, ct));

    [HttpGet("categorias")]
    public async Task<ActionResult<PaginatedResult<AuditoriaCategoriaResponse>>> Categorias([FromQuery] AuditoriaBusquedaFiltro filtro, CancellationToken ct) =>
        Ok(await auditoria.CategoriasAsync(filtro, ct));
    [HttpGet("categorias/{id:int}")]
    public async Task<ActionResult<AuditoriaCategoriaResponse>> Categoria(int id, CancellationToken ct) =>
        Ok(await auditoria.CategoriaAsync(id, ct));

    [HttpGet("proveedores")]
    public async Task<ActionResult<PaginatedResult<AuditoriaProveedorResponse>>> Proveedores([FromQuery] AuditoriaBusquedaFiltro filtro, CancellationToken ct) =>
        Ok(await auditoria.ProveedoresAsync(filtro, ct));
    [HttpGet("proveedores/{id:int}")]
    public async Task<ActionResult<AuditoriaProveedorResponse>> Proveedor(int id, CancellationToken ct) =>
        Ok(await auditoria.ProveedorAsync(id, ct));

    [HttpGet("productos")]
    public async Task<ActionResult<PaginatedResult<AuditoriaProductoResponse>>> Productos([FromQuery] AuditoriaProductosFiltro filtro, CancellationToken ct) =>
        Ok(await auditoria.ProductosAsync(filtro, ct));
    [HttpGet("productos/{id:int}")]
    public async Task<ActionResult<AuditoriaProductoResponse>> Producto(int id, CancellationToken ct) =>
        Ok(await auditoria.ProductoAsync(id, ct));
    [HttpGet("productos/{id:int}/movimientos")]
    public async Task<ActionResult<PaginatedResult<AuditoriaMovimientoResponse>>> Movimientos(int id, [FromQuery] AuditoriaMovimientosFiltro filtro, CancellationToken ct) =>
        Ok(await auditoria.MovimientosAsync(id, filtro, ct));

    [HttpGet("ingresos")]
    public async Task<ActionResult<PaginatedResult<AuditoriaIngresoResumen>>> Ingresos([FromQuery] AuditoriaIngresosFiltro filtro, CancellationToken ct) =>
        Ok(await auditoria.IngresosAsync(filtro, ct));
    [HttpGet("ingresos/{id:int}")]
    public async Task<ActionResult<AuditoriaIngresoResponse>> Ingreso(int id, CancellationToken ct) =>
        Ok(await auditoria.IngresoAsync(id, ct));

    [HttpGet("salidas")]
    public async Task<ActionResult<PaginatedResult<AuditoriaSalidaResumen>>> Salidas([FromQuery] AuditoriaSalidasFiltro filtro, CancellationToken ct) =>
        Ok(await auditoria.SalidasAsync(filtro, ct));
    [HttpGet("salidas/{id:int}")]
    public async Task<ActionResult<AuditoriaSalidaResponse>> Salida(int id, CancellationToken ct) =>
        Ok(await auditoria.SalidaAsync(id, ct));

    [HttpGet("acciones")]
    public async Task<IActionResult> GetAcciones([FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        [FromQuery] int? usuarioId = null, [FromQuery] string? entidad = null, [FromQuery] string? accion = null,
        [FromQuery] DateTime? desde = null, [FromQuery] DateTime? hasta = null, CancellationToken ct = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? 20 : pageSize > 100 ? 100 : pageSize;
        var query = db.AuditoriaAcciones.AsNoTracking().AsQueryable();
        if (usuarioId.HasValue) query = query.Where(a => a.UsuarioId == usuarioId.Value);
        if (!string.IsNullOrWhiteSpace(entidad)) query = query.Where(a => a.Entidad == entidad.Trim());
        if (!string.IsNullOrWhiteSpace(accion)) query = query.Where(a => a.Accion == accion.Trim());
        if (desde.HasValue) query = query.Where(a => a.FechaUtc >= desde.Value);
        if (hasta.HasValue) query = query.Where(a => a.FechaUtc <= hasta.Value);
        var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(a => a.FechaUtc).ThenByDescending(a => a.Id)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(a => new
            {
                a.Id,
                a.UsuarioId,
                a.UsuarioEmail,
                a.Accion,
                a.Entidad,
                a.EntidadId,
                a.FechaUtc,
                a.Ip,
                a.Observacion
            })
            .ToListAsync(ct);
        return Ok(new { items, total, page, pageSize });
    }

    [HttpGet("acciones/{id:long}")]
    public async Task<IActionResult> GetAccion(long id, CancellationToken ct)
    {
        var item = await db.AuditoriaAcciones.AsNoTracking().Where(a => a.Id == id)
            .Select(a => new
            {
                a.Id,
                a.UsuarioId,
                a.UsuarioEmail,
                a.Accion,
                a.Entidad,
                a.EntidadId,
                a.FechaUtc,
                a.Ip,
                a.Observacion
            })
            .FirstOrDefaultAsync(ct);
        if (item is null) return NotFound();
        return Ok(item);
    }
}
