using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TPN_5_MOVIL.Data;
using TPN_5_MOVIL.DTOs;
using TPN_5_MOVIL.Entities;
using TPN_5_MOVIL.Services;

namespace TPN_5_MOVIL.Controllers;

[ApiController]
[Route("api/salidas")]
// Client ownership queries are introduced with the complete checkout workflow.
[Authorize(Roles = Usuario.RolAdministrador + "," + Usuario.RolEmpleado)]
public class SalidasController(AppDbContext db, MovimientoService movimientos) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PaginatedResult<SalidaResponse>>> GetAll(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 10,
        [FromQuery] int? productoId = null, [FromQuery] int? clienteId = null,
        [FromQuery] DateTime? desde = null, [FromQuery] DateTime? hasta = null)
    {
        var query = db.Salidas.AsNoTracking();
        if (productoId.HasValue) query = query.Where(x => x.Detalles.Any(d => d.ProductoId == productoId.Value));
        if (clienteId.HasValue) query = query.Where(x => x.ClienteId == clienteId.Value);
        if (desde.HasValue) query = query.Where(x => x.Fecha >= desde.Value);
        if (hasta.HasValue) query = query.Where(x => x.Fecha <= hasta.Value);
        return Ok(await PaginatedResult<SalidaResponse>.CreateAsync(
            query.OrderByDescending(x => x.Fecha).ThenByDescending(x => x.Id).Select(x => new SalidaResponse(x.Id, x.ClienteId, x.EmpleadoId, x.Fecha, x.Observaciones, x.ConfirmarXCliente, x.ConfirmarXEmpleado, x.FechaConfirmacionCliente, x.FechaConfirmacionEmpleado,
                x.Detalles.OrderBy(d => d.Id).Select(d => new DetalleSalidaResponse(d.Id, d.ProductoId, d.Cantidad, d.PrecioUnitario, d.DescAplicado)).ToList())), page, pageSize));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<SalidaResponse>> GetById(int id)
    {
        var result = await db.Salidas.AsNoTracking().Where(x => x.Id == id).Select(x => new SalidaResponse(x.Id, x.ClienteId, x.EmpleadoId, x.Fecha, x.Observaciones, x.ConfirmarXCliente, x.ConfirmarXEmpleado, x.FechaConfirmacionCliente, x.FechaConfirmacionEmpleado,
                x.Detalles.OrderBy(d => d.Id).Select(d => new DetalleSalidaResponse(d.Id, d.ProductoId, d.Cantidad, d.PrecioUnitario, d.DescAplicado)).ToList())).FirstOrDefaultAsync();
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<SalidaResponse>> Create([FromBody] SalidaRequest request)
    {
        var result = await movimientos.CrearSalidaAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }
}
