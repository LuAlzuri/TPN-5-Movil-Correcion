using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
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
[Route("api/usuarios")]
[Authorize(Roles = Usuario.RolAdministrador)]
public class UsuariosController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly PasswordHasher<Usuario> _passwordHasher = new();

    public UsuariosController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<PaginatedResult<UsuarioDto>>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? busqueda = null)
    {
        var query = _db.Usuarios
            .AsNoTracking()
            .OrderBy(u => u.Email)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(busqueda))
            query = query.Where(u => (u.Nombre + " " + u.Apellido).Contains(busqueda) || u.Email.Contains(busqueda));

        var result = await PaginatedResult<UsuarioDto>.CreateAsync(
            query.Select(u => new UsuarioDto(u.Id, u.Nombre, u.Apellido, u.CUIT, u.Email, u.Rol, u.Eliminado)),
            page,
            pageSize);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<UsuarioDto>> GetById(int id)
    {
        var usuario = await _db.Usuarios
            .AsNoTracking()
            .Where(u => u.Id == id)
            .Select(u => new UsuarioDto(u.Id, u.Nombre, u.Apellido, u.CUIT, u.Email, u.Rol, u.Eliminado))
            .FirstOrDefaultAsync();

        return usuario is null ? NotFound() : Ok(usuario);
    }

    [HttpPost]
    public ActionResult<UsuarioDto> Create([FromBody] UsuarioCreateRequest request)
    {
        throw new ApiException("La creación conjunta de cuentas y perfiles está pendiente de implementación.", StatusCodes.Status501NotImplemented);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UsuarioUpdateRequest request)
    {
        var usuario = await _db.Usuarios.FindAsync(id);
        if (usuario is null)
            return NotFound();

        if (await _db.Usuarios.IgnoreQueryFilters().AnyAsync(u => u.Email == request.Email && u.Id != id))
            throw new ApiException("Ya existe un usuario con ese email.", StatusCodes.Status409Conflict);

        if (usuario.Rol == Usuario.RolCliente)
            await CuitUnico.ValidarClienteAsync(_db, request.CUIT, id);

        usuario.Nombre = request.Nombre;
        usuario.Apellido = request.Apellido;
        usuario.CUIT = request.CUIT.Trim();
        usuario.Email = request.Email;
        if (!string.IsNullOrWhiteSpace(request.Password))
        {
            AuthService.ValidarPassword(request.Password);
            usuario.PasswordHash = _passwordHasher.HashPassword(usuario, request.Password);
        }

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
    public async Task<IActionResult> Delete(int id)
    {
        var usuario = await _db.Usuarios.FindAsync(id);
        if (usuario is null)
            return NotFound();

        usuario.Eliminado = true;
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
