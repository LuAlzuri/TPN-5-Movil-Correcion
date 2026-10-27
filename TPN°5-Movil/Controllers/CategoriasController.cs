using TPN_5_MOVIL.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TPN_5_MOVIL.Data;
using TPN_5_MOVIL.DTOs;
using TPN_5_MOVIL.Entities;
using TPN_5_MOVIL.Exceptions;

namespace TPN_5_MOVIL.Controllers;

[ApiController]
[Route("api/categorias")]
[Authorize]
public class CategoriasController : ControllerBase
{
    private readonly AppDbContext _db;

    public CategoriasController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    [Authorize(Roles = Usuario.RolAdministrador)]
    public async Task<ActionResult<PaginatedResult<CategoriaResponse>>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? busqueda = null)
    {
        Paginacion.Validar(page, pageSize);

        busqueda = busqueda?.Trim();
        var query = _db.Categorias
            .AsNoTracking()
            .OrderBy(c => c.Nombre).ThenBy(c => c.Id)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(busqueda))
            query = query.Where(c => c.Nombre.Contains(busqueda));

        var result = await PaginatedResult<CategoriaResponse>.CreateAsync(
            query.Select(c => new CategoriaResponse(c.Id, c.Nombre, c.Descripcion, c.FechaCreacion)),
            page,
            pageSize);
        return Ok(result);
    }

    [HttpGet("catalogo")]
    [Authorize(Roles = GruposRoles.Todos)]
    public async Task<ActionResult<IEnumerable<CategoriaCatalogoResponse>>> GetCatalogo()
    {
        var categorias = await _db.Categorias
            .AsNoTracking()
            .OrderBy(c => c.Nombre).ThenBy(c => c.Id)
            .Select(c => new CategoriaCatalogoResponse(c.Id, c.Nombre))
            .ToListAsync();
        return Ok(categorias);
    }

    [HttpGet("{id:int}")]
    [Authorize(Roles = Usuario.RolAdministrador)]
    public async Task<ActionResult<CategoriaResponse>> GetById(int id)
    {
        var categoria = await _db.Categorias
            .AsNoTracking()
            .Where(c => c.Id == id)
            .Select(c => new CategoriaResponse(c.Id, c.Nombre, c.Descripcion, c.FechaCreacion))
            .FirstOrDefaultAsync();

        return categoria is null ? NotFound() : Ok(categoria);
    }

    [HttpPost]
    [Authorize(Roles = Usuario.RolAdministrador)]
    public async Task<ActionResult<CategoriaResponse>> Create([FromBody] CategoriaCreateRequest request)
    {
        if (await _db.Categorias.IgnoreQueryFilters().AnyAsync(c => c.Nombre == request.Nombre.Trim()))
            throw new ApiException("Ya existe una categoría con ese nombre.", StatusCodes.Status409Conflict);

        var categoria = new Categoria
        {
            Nombre = request.Nombre.Trim(),
            Descripcion = Normalizacion.Opcional(request.Descripcion),
            FechaCreacion = DateTime.UtcNow
        };

        _db.Categorias.Add(categoria);
        await GuardarAsync();

        return CreatedAtAction(nameof(GetById), new { id = categoria.Id },
            new CategoriaResponse(categoria.Id, categoria.Nombre, categoria.Descripcion, categoria.FechaCreacion));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = Usuario.RolAdministrador)]
    public async Task<IActionResult> Update(int id, [FromBody] CategoriaUpdateRequest request)
    {
        var categoria = await _db.Categorias.FindAsync(id);
        if (categoria is null)
            return NotFound();

        if (await _db.Categorias.IgnoreQueryFilters().AnyAsync(c => c.Nombre == request.Nombre.Trim() && c.Id != id))
            throw new ApiException("Ya existe una categoría con ese nombre.", StatusCodes.Status409Conflict);

        categoria.Nombre = request.Nombre.Trim();
        categoria.Descripcion = Normalizacion.Opcional(request.Descripcion);

        await GuardarAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = Usuario.RolAdministrador)]
    public async Task<IActionResult> Delete(int id)
    {
        var categoria = await _db.Categorias.FindAsync(id);
        if (categoria is null)
            return NotFound();

        categoria.Eliminado = true;
        await GuardarAsync();
        return NoContent();
    }

    private async Task GuardarAsync()
    {
        await _db.GuardarOConflictoAsync("Ya existe una categoría con ese nombre.");
    }
}
