using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TPN_5_MOVIL.Data;
using TPN_5_MOVIL.DTOs;
using TPN_5_MOVIL.Entities;
using TPN_5_MOVIL.Services;

namespace TPN_5_MOVIL.Controllers;

[ApiController]
[Route("api/ingresos")]
// Client ownership queries are introduced with the complete checkout workflow.
[Authorize(Roles = Usuario.RolAdministrador + "," + Usuario.RolEmpleado)]
public class IngresosController(AppDbContext db, MovimientoService movimientos) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PaginatedResult<IngresoResponse>>> GetAll(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 10,
        [FromQuery] int? productoId = null, [FromQuery] int? proveedorId = null,
        [FromQuery] DateTime? desde = null, [FromQuery] DateTime? hasta = null)
    {
        var query = db.Ingresos.AsNoTracking();
        if (productoId.HasValue) query = query.Where(x => x.Detalles.Any(d => d.ProductoId == productoId.Value));
        if (proveedorId.HasValue) query = query.Where(x => x.ProveedorId == proveedorId.Value);
        if (desde.HasValue) query = query.Where(x => x.Fecha >= desde.Value);
        if (hasta.HasValue) query = query.Where(x => x.Fecha <= hasta.Value);
        return Ok(await PaginatedResult<IngresoResponse>.CreateAsync(
            query.OrderByDescending(x => x.Fecha).ThenByDescending(x => x.Id).Select(x => new IngresoResponse(x.Id, x.ProveedorId, x.EmpleadoId, x.Fecha, x.Observaciones, x.Confirmado, x.FechaConfirmacion, x.EmpleadoConfirmacionId,
                x.Detalles.OrderBy(d => d.Id).Select(d => new DetalleIngresoResponse(d.Id, d.ProductoId, d.Cantidad, d.PrecioUnitario, d.DescAplicado)).ToList())), page, pageSize));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<IngresoResponse>> GetById(int id)
    {
        var result = await db.Ingresos.AsNoTracking().Where(x => x.Id == id).Select(x => new IngresoResponse(x.Id, x.ProveedorId, x.EmpleadoId, x.Fecha, x.Observaciones, x.Confirmado, x.FechaConfirmacion, x.EmpleadoConfirmacionId,
                x.Detalles.OrderBy(d => d.Id).Select(d => new DetalleIngresoResponse(d.Id, d.ProductoId, d.Cantidad, d.PrecioUnitario, d.DescAplicado)).ToList())).FirstOrDefaultAsync();
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<IngresoResponse>> Create([FromBody] IngresoRequest request)
    {
        var result = await movimientos.CrearIngresoAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }
}
