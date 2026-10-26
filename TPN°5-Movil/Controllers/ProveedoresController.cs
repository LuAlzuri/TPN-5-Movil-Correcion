using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TPN_5_MOVIL.Data;
using TPN_5_MOVIL.DTOs;
using TPN_5_MOVIL.Entities;
using TPN_5_MOVIL.Exceptions;

namespace TPN_5_MOVIL.Controllers;

[ApiController]
[Route("api/proveedores")]
[Authorize]
public class ProveedoresController : ControllerBase
{
    private readonly AppDbContext _db;

    public ProveedoresController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<PaginatedResult<ProveedorResponse>>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? busqueda = null)
    {
        var query = _db.Proveedores
            .AsNoTracking()
            .OrderBy(p => p.Nombre)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(busqueda))
            query = query.Where(p => p.Nombre.Contains(busqueda) || p.CUIT.Contains(busqueda) || p.Email!.Contains(busqueda));

        var result = await PaginatedResult<ProveedorResponse>.CreateAsync(
            query.Select(p => new ProveedorResponse(p.Id, p.Nombre, p.CUIT, p.Email, p.Telefono, p.Direccion)),
            page,
            pageSize);
        return Ok(result);
    }

    [HttpGet("catalogo")]
    public async Task<ActionResult<IEnumerable<ProveedorResponse>>> GetCatalogo()
    {
        var proveedores = await _db.Proveedores
            .AsNoTracking()
            .OrderBy(p => p.Nombre)
            .Select(p => new ProveedorResponse(p.Id, p.Nombre, p.CUIT, p.Email, p.Telefono, p.Direccion))
            .ToListAsync();
        return Ok(proveedores);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProveedorResponse>> GetById(int id)
    {
        var proveedor = await _db.Proveedores
            .AsNoTracking()
            .Where(p => p.Id == id)
            .Select(p => new ProveedorResponse(p.Id, p.Nombre, p.CUIT, p.Email, p.Telefono, p.Direccion))
            .FirstOrDefaultAsync();

        return proveedor is null ? NotFound() : Ok(proveedor);
    }

    [HttpPost]
    [Authorize(Roles = Usuario.RolAdministrador + "," + Usuario.RolEmpleado)]
    public async Task<ActionResult<ProveedorResponse>> Create([FromBody] ProveedorRequest request)
    {
        if (await _db.Proveedores.IgnoreQueryFilters().AnyAsync(p => p.CUIT == request.CUIT))
            throw new ApiException("Ya existe un proveedor con ese CUIT.", StatusCodes.Status409Conflict);

        var proveedor = new Proveedor
        {
            Nombre = request.Nombre,
            CUIT = request.CUIT,
            Email = request.Email,
            Telefono = request.Telefono,
            Direccion = request.Direccion
        };

        _db.Proveedores.Add(proveedor);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = proveedor.Id },
            new ProveedorResponse(proveedor.Id, proveedor.Nombre, proveedor.CUIT, proveedor.Email, proveedor.Telefono, proveedor.Direccion));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = Usuario.RolAdministrador + "," + Usuario.RolEmpleado)]
    public async Task<IActionResult> Update(int id, [FromBody] ProveedorRequest request)
    {
        var proveedor = await _db.Proveedores.FindAsync(id);
        if (proveedor is null)
            return NotFound();

        if (await _db.Proveedores.IgnoreQueryFilters().AnyAsync(p => p.CUIT == request.CUIT && p.Id != id))
            throw new ApiException("Ya existe un proveedor con ese CUIT.", StatusCodes.Status409Conflict);

        proveedor.Nombre = request.Nombre;
        proveedor.CUIT = request.CUIT;
        proveedor.Email = request.Email;
        proveedor.Telefono = request.Telefono;
        proveedor.Direccion = request.Direccion;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = Usuario.RolAdministrador + "," + Usuario.RolEmpleado)]
    public async Task<IActionResult> Delete(int id)
    {
        var proveedor = await _db.Proveedores.FindAsync(id);
        if (proveedor is null)
            return NotFound();

        proveedor.Eliminado = true;
        await _db.SaveChangesAsync();
        return NoContent();
    }
}