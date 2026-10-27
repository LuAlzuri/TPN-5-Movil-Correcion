using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TPN_5_MOVIL.DTOs;
using TPN_5_MOVIL.Entities;
using TPN_5_MOVIL.Security;
using TPN_5_MOVIL.Services;

namespace TPN_5_MOVIL.Controllers;

[ApiController]
[Route("api/salidas")]
[Authorize(Roles = Usuario.RolAdministrador + "," + Usuario.RolEmpleado + "," + Usuario.RolCliente)]
public class SalidasController(SalidaService salidas) : ControllerBase
{
    private int? PropietarioId => User.IsInRole(Usuario.RolCliente) ? User.ObtenerClienteId() : null;

    [HttpGet]
    public async Task<ActionResult<PaginatedResult<SalidaResponse>>> GetAll(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 10,
        [FromQuery] int? productoId = null, [FromQuery] int? clienteId = null,
        [FromQuery] DateTime? desde = null, [FromQuery] DateTime? hasta = null,
        CancellationToken cancellationToken = default) =>
        Ok(await salidas.ListarAsync(PropietarioId, page, pageSize, productoId, clienteId, desde, hasta, cancellationToken));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<SalidaResponse>> GetById(int id, CancellationToken cancellationToken) =>
        Ok(await salidas.ConsultarAsync(id, PropietarioId, cancellationToken));

    [HttpGet("{id:int}/detalles/{detalleId:int}")]
    public async Task<ActionResult<DetalleSalidaResponse>> GetDetalle(int id, int detalleId, CancellationToken cancellationToken) =>
        Ok(await salidas.ConsultarDetalleAsync(id, detalleId, PropietarioId, cancellationToken));

    [HttpPost]
    [Authorize(Roles = Usuario.RolCliente)]
    public async Task<ActionResult<SalidaResponse>> Create([FromBody] SalidaRequest request, CancellationToken cancellationToken)
    {
        var result = await salidas.CrearAsync(request, User.ObtenerClienteId(), cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPost("{id:int}/detalles")]
    [Authorize(Roles = Usuario.RolCliente)]
    public async Task<ActionResult<DetalleSalidaResponse>> AgregarDetalle(int id, [FromBody] DetalleSalidaCreateRequest request,
        CancellationToken cancellationToken)
    {
        var (detalle, creado) = await salidas.AgregarDetalleAsync(id, request, User.ObtenerClienteId(), cancellationToken);
        if (detalle is null) return NoContent();
        return creado ? CreatedAtAction(nameof(GetDetalle), new { id, detalleId = detalle.Id }, detalle) : Ok(detalle);
    }

    [HttpPut("{id:int}/detalles/{detalleId:int}")]
    [Authorize(Roles = Usuario.RolCliente)]
    public async Task<ActionResult<DetalleSalidaResponse>> EditarDetalle(int id, int detalleId,
        [FromBody] DetalleSalidaUpdateRequest request, CancellationToken cancellationToken)
    {
        var detalle = await salidas.EditarDetalleAsync(id, detalleId, request, User.ObtenerClienteId(), cancellationToken);
        return detalle is null ? NoContent() : Ok(detalle);
    }

    [HttpDelete("{id:int}/detalles/{detalleId:int}")]
    [Authorize(Roles = Usuario.RolCliente)]
    public async Task<IActionResult> QuitarDetalle(int id, int detalleId, CancellationToken cancellationToken)
    {
        await salidas.QuitarDetalleAsync(id, detalleId, User.ObtenerClienteId(), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:int}/confirmar-cliente")]
    [Authorize(Roles = Usuario.RolCliente)]
    public async Task<ActionResult<SalidaResponse>> ConfirmarCliente(int id, CancellationToken cancellationToken) =>
        Ok(await salidas.ConfirmarClienteAsync(id, User.ObtenerClienteId(), cancellationToken));

    [HttpPost("{id:int}/confirmar")]
    [Authorize(Roles = Usuario.RolAdministrador + "," + Usuario.RolEmpleado)]
    public async Task<ActionResult<SalidaResponse>> Confirmar(int id, CancellationToken cancellationToken) =>
        Ok(await salidas.ConfirmarAsync(id, User.ObtenerUsuarioId(), cancellationToken));

    [HttpDelete("{id:int}")]
    [Authorize(Roles = Usuario.RolCliente)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await salidas.EliminarAsync(id, User.ObtenerClienteId(), cancellationToken);
        return NoContent();
    }
}
