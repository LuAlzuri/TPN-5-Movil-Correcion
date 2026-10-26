using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
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
        if (page < 1 || pageSize < 1 || pageSize > 100 || (long)(page - 1) * pageSize > int.MaxValue)
            throw new ApiException("Página inválida: page debe ser positivo y pageSize debe estar entre 1 y 100.");

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
        var telefono = NormalizarOpcional(request.Telefono);
        var direccion = NormalizarOpcional(request.Direccion);
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

        var email = request.Email.Trim().ToLowerInvariant();
        var comparison = email.ToUpperInvariant();
        if (await db.Usuarios.IgnoreQueryFilters().AnyAsync(u => u.Email.ToUpper() == comparison && u.Id != id))
            throw new ApiException("Ya existe un usuario con ese email.", StatusCodes.Status409Conflict);

        cliente.Usuario.Nombre = request.Nombre.Trim();
        cliente.Usuario.Apellido = request.Apellido.Trim();
        cliente.Usuario.CUIT = request.CUIT.Trim();
        cliente.Usuario.Email = email;
        cliente.Telefono = NormalizarOpcional(request.Telefono);
        cliente.Direccion = NormalizarOpcional(request.Direccion);

        try
        {
            // SaveChanges commits the Usuario and Cliente updates atomically.
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 })
        {
            throw new ApiException("Ya existe una cuenta con esos datos únicos.", StatusCodes.Status409Conflict);
        }
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

    private static string? NormalizarOpcional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
