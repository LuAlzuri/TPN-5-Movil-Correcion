using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TPN_5_MOVIL.DTOs;
using TPN_5_MOVIL.Entities;
using TPN_5_MOVIL.Security;
using TPN_5_MOVIL.Services;

namespace TPN_5_MOVIL.Controllers;

[ApiController]
[Route("api/ingresos")]
[Authorize(Roles = Usuario.RolAdministrador + "," + Usuario.RolEmpleado)]
public class IngresosController(IngresoService ingresos) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PaginatedResult<IngresoResponse>>> GetAll(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 10,
        [FromQuery] int? productoId = null, [FromQuery] int? proveedorId = null,
        [FromQuery] DateTime? desde = null, [FromQuery] DateTime? hasta = null,
        CancellationToken cancellationToken = default) =>
        Ok(await ingresos.ListarAsync(page, pageSize, productoId, proveedorId, desde, hasta, cancellationToken));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<IngresoResponse>> GetById(int id, CancellationToken cancellationToken) =>
        Ok(await ingresos.ConsultarAsync(id, cancellationToken));

    [HttpGet("{id:int}/detalles/{detalleId:int}")]
    public async Task<ActionResult<DetalleIngresoResponse>> GetDetalle(int id, int detalleId, CancellationToken cancellationToken) =>
        Ok(await ingresos.ConsultarDetalleAsync(id, detalleId, cancellationToken));

    [HttpPost]
    [Authorize(Roles = Usuario.RolEmpleado)]
    public async Task<ActionResult<IngresoResponse>> Create([FromBody] IngresoRequest request, CancellationToken cancellationToken)
    {
        var result = await ingresos.CrearAsync(request, User.ObtenerEmpleadoId(), cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPost("{id:int}/detalles")]
    [Authorize(Roles = Usuario.RolEmpleado)]
    public async Task<ActionResult<DetalleIngresoResponse>> AgregarDetalle(int id, [FromBody] DetalleIngresoCreateRequest request,
        CancellationToken cancellationToken)
    {
        var result = await ingresos.AgregarDetalleAsync(id, request, User.ObtenerEmpleadoId(), cancellationToken);
        return CreatedAtAction(nameof(GetDetalle), new { id, detalleId = result.Id }, result);
    }

    [HttpPut("{id:int}/detalles/{detalleId:int}")]
    [Authorize(Roles = Usuario.RolEmpleado)]
    public async Task<ActionResult<DetalleIngresoResponse>> EditarDetalle(int id, int detalleId,
        [FromBody] DetalleIngresoUpdateRequest request, CancellationToken cancellationToken) =>
        Ok(await ingresos.EditarDetalleAsync(id, detalleId, request, User.ObtenerEmpleadoId(), cancellationToken));

    [HttpDelete("{id:int}/detalles/{detalleId:int}")]
    [Authorize(Roles = Usuario.RolEmpleado)]
    public async Task<IActionResult> QuitarDetalle(int id, int detalleId, CancellationToken cancellationToken)
    {
        await ingresos.QuitarDetalleAsync(id, detalleId, User.ObtenerEmpleadoId(), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:int}/confirmar")]
    [Authorize(Roles = Usuario.RolEmpleado)]
    public async Task<ActionResult<IngresoResponse>> Confirmar(int id, CancellationToken cancellationToken) =>
        Ok(await ingresos.ConfirmarAsync(id, User.ObtenerEmpleadoId(), cancellationToken));

    [HttpDelete("{id:int}")]
    [Authorize(Roles = Usuario.RolEmpleado)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await ingresos.EliminarAsync(id, User.ObtenerEmpleadoId(), cancellationToken);
        return NoContent();
    }
}
