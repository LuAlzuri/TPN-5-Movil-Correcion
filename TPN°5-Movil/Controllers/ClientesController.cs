using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TPN_5_MOVIL.Data;
using TPN_5_MOVIL.DTOs;
using TPN_5_MOVIL.Entities;
using TPN_5_MOVIL.Exceptions;
using TPN_5_MOVIL.Services;

namespace TPN_5_MOVIL.Controllers;

[ApiController]
[Route("api/clientes")]
[Authorize(Roles = Usuario.RolAdministrador)]
public class ClientesController(AppDbContext db, AuthService auth) : ControllerBase
{
    private IQueryable<Cliente> ClientesValidos => db.Clientes
        .Where(c => c.Usuario.Rol == Usuario.RolCliente && c.Usuario.Empleado == null);

    private static IQueryable<ClienteResponse> Respuestas(IQueryable<Cliente> query) =>
        query.Select(c => new ClienteResponse(c.Id, c.Usuario.Nombre, c.Usuario.Apellido,
            c.Usuario.CUIT, c.Usuario.Email, c.Telefono, c.Direccion));

    [HttpGet]
    public async Task<ActionResult<PaginatedResult<ClienteResponse>>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? busqueda = null)
    {
        Paginacion.Validar(page, pageSize);

        var query = ClientesValidos.AsNoTracking();
        busqueda = busqueda?.Trim();

        if (!string.IsNullOrWhiteSpace(busqueda))
            query = query.Where(c => (c.Usuario.Nombre + " " + c.Usuario.Apellido).Contains(busqueda)
                || c.Usuario.CUIT.Contains(busqueda) || c.Usuario.Email.Contains(busqueda));

        var result = await PaginatedResult<ClienteResponse>.CreateAsync(
            Respuestas(query.OrderBy(c => c.Usuario.Apellido).ThenBy(c => c.Usuario.Nombre).ThenBy(c => c.Id)),
            page,
            pageSize);
        return Ok(result);
    }

    [HttpGet("catalogo")]
    public async Task<ActionResult<IEnumerable<ClienteResponse>>> GetCatalogo()
    {
        var clientes = await Respuestas(ClientesValidos.AsNoTracking()
            .OrderBy(c => c.Usuario.Apellido).ThenBy(c => c.Usuario.Nombre).ThenBy(c => c.Id))
            .ToListAsync();
        return Ok(clientes);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ClienteResponse>> GetById(int id)
    {
        var cliente = await Respuestas(ClientesValidos.AsNoTracking().Where(c => c.Id == id))
            .FirstOrDefaultAsync();

        return cliente is null ? NotFound() : Ok(cliente);
    }

    [HttpPost]
    public async Task<ActionResult<ClienteResponse>> Create([FromBody] ClienteCreateRequest request)
    {
        CuitUnico.Normalizar(request.CUIT);
        var telefono = Normalizacion.Opcional(request.Telefono);
        var direccion = Normalizacion.Opcional(request.Direccion);
        var usuario = await auth.RegisterAsync(new RegisterRequest
        {
            Nombre = request.Nombre, Apellido = request.Apellido, CUIT = request.CUIT,
            Email = request.Email, Password = request.Password, Rol = Usuario.RolCliente,
            Telefono = telefono, Direccion = direccion
        });
        return CreatedAtAction(nameof(GetById), new { id = usuario.Id },
            new ClienteResponse(usuario.Id, usuario.Nombre, usuario.Apellido, usuario.CUIT,
                usuario.Email, telefono, direccion));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] ClienteUpdateRequest request)
    {
        var cliente = await ClientesValidos.Include(c => c.Usuario).FirstOrDefaultAsync(c => c.Id == id);
        if (cliente is null)
            return NotFound();

        await CuitUnico.ValidarClienteAsync(db, request.CUIT, id);

        var email = Normalizacion.Email(request.Email);
        await db.ExigirEmailLibreAsync(email, "Ya existe un usuario con ese email.", id);

        AuthService.AsignarDatos(cliente.Usuario, request.Nombre, request.Apellido, request.CUIT, email);
        cliente.Telefono = Normalizacion.Opcional(request.Telefono);
        cliente.Direccion = Normalizacion.Opcional(request.Direccion);

        await db.GuardarOConflictoAsync("Ya existe una cuenta con esos datos únicos.");
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var cliente = await ClientesValidos.Include(c => c.Usuario).FirstOrDefaultAsync(c => c.Id == id);
        if (cliente is null)
            return NotFound();

        cliente.Usuario.Eliminado = true;
        await db.SaveChangesAsync();
        return NoContent();
    }
}
