using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TPN_5_MOVIL.DTOs;
using TPN_5_MOVIL.Entities;
using TPN_5_MOVIL.Services;

namespace TPN_5_MOVIL.Controllers;

[ApiController]
[Route("api/auditoria")]
[Authorize(Roles = Usuario.RolAdministrador)]
public sealed class AuditoriaController(AuditoriaService auditoria) : ControllerBase
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
}
