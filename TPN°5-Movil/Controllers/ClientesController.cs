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
public class ClientesController : ControllerBase
{
    private readonly AppDbContext _db;

    public ClientesController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<PaginatedResult<ClienteResponse>>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? busqueda = null)
    {
        var query = _db.Clientes
            .AsNoTracking()
            .OrderBy(c => c.Usuario.Nombre)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(busqueda))
            query = query.Where(c => c.Usuario.Nombre.Contains(busqueda) || c.Usuario.CUIT.Contains(busqueda) || c.Usuario.Email!.Contains(busqueda));

        var result = await PaginatedResult<ClienteResponse>.CreateAsync(
            query.Select(c => new ClienteResponse(c.Id, c.Usuario.Nombre, c.Usuario.Apellido, c.Usuario.CUIT, c.Usuario.Email, c.Telefono, c.Direccion)),
            page,
            pageSize);
        return Ok(result);
    }

    [HttpGet("catalogo")]
    public async Task<ActionResult<IEnumerable<ClienteResponse>>> GetCatalogo()
    {
        var clientes = await _db.Clientes
            .AsNoTracking()
            .OrderBy(c => c.Usuario.Nombre)
            .Select(c => new ClienteResponse(c.Id, c.Usuario.Nombre, c.Usuario.Apellido, c.Usuario.CUIT, c.Usuario.Email, c.Telefono, c.Direccion))
            .ToListAsync();
        return Ok(clientes);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ClienteResponse>> GetById(int id)
    {
        var cliente = await _db.Clientes
            .AsNoTracking()
            .Where(c => c.Id == id)
            .Select(c => new ClienteResponse(c.Id, c.Usuario.Nombre, c.Usuario.Apellido, c.Usuario.CUIT, c.Usuario.Email, c.Telefono, c.Direccion))
            .FirstOrDefaultAsync();

        return cliente is null ? NotFound() : Ok(cliente);
    }

    [HttpPost]
    [Authorize(Roles = Usuario.RolAdministrador)]
    public ActionResult<ClienteResponse> Create([FromBody] ClienteRequest request)
    {
        throw new ApiException("La creación conjunta de clientes y cuentas está pendiente de implementación.", StatusCodes.Status501NotImplemented);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = Usuario.RolAdministrador)]
    public async Task<IActionResult> Update(int id, [FromBody] ClienteRequest request)
    {
        var cliente = await _db.Clientes.Include(c => c.Usuario).FirstOrDefaultAsync(c => c.Id == id);
        if (cliente is null)
            return NotFound();

        await CuitUnico.ValidarClienteAsync(_db, request.CUIT, id);

        if (await _db.Usuarios.IgnoreQueryFilters().AnyAsync(u => u.Email == request.Email && u.Id != id))
            throw new ApiException("Ya existe un usuario con ese email.", StatusCodes.Status409Conflict);

        cliente.Usuario.Nombre = request.Nombre;
        cliente.Usuario.Apellido = request.Apellido;
        cliente.Usuario.CUIT = request.CUIT.Trim();
        cliente.Usuario.Email = request.Email;
        cliente.Telefono = request.Telefono;
        cliente.Direccion = request.Direccion;

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 })
        {
            throw new ApiException("Ya existe una cuenta con esos datos únicos.", StatusCodes.Status409Conflict);
        }
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = Usuario.RolAdministrador)]
    public async Task<IActionResult> Delete(int id)
    {
        var cliente = await _db.Clientes.Include(c => c.Usuario).FirstOrDefaultAsync(c => c.Id == id);
        if (cliente is null)
            return NotFound();

        cliente.Usuario.Eliminado = true;
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
