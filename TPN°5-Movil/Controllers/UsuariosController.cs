using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TPN_5_MOVIL.Data;
using TPN_5_MOVIL.DTOs;
using TPN_5_MOVIL.Entities;
using TPN_5_MOVIL.Exceptions;
using TPN_5_MOVIL.Services;
using TPN_5_MOVIL.Security;

namespace TPN_5_MOVIL.Controllers;

[ApiController]
[Route("api/usuarios")]
[Authorize(Roles = Usuario.RolAdministrador)]
public class UsuariosController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly AuthService _auth;
    private readonly PasswordHasher<Usuario> _passwordHasher = new();

    public UsuariosController(AppDbContext db, AuthService auth)
    {
        _db = db;
        _auth = auth;
    }

    [HttpGet]
    public async Task<ActionResult<PaginatedResult<UsuarioDto>>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? busqueda = null)
    {
        Paginacion.Validar(page, pageSize);

        var query = _db.Usuarios
            .AsNoTracking()
            .AsQueryable();

        busqueda = busqueda?.Trim();
        if (!string.IsNullOrWhiteSpace(busqueda))
            query = query.Where(u => (u.Nombre + " " + u.Apellido).Contains(busqueda) || u.Email.Contains(busqueda));

        var result = await PaginatedResult<UsuarioDto>.CreateAsync(
            query.OrderBy(u => u.Email).ThenBy(u => u.Id)
                .Select(u => new UsuarioDto(u.Id, u.Nombre, u.Apellido, u.CUIT, u.Email, u.Rol, u.Eliminado)),
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
    public async Task<ActionResult<UsuarioDto>> Create([FromBody] UsuarioCreateRequest request, CancellationToken cancellationToken)
    {
        var usuario = await _auth.RegisterAsync(new RegisterRequest
        {
            Nombre = request.Nombre, Apellido = request.Apellido, CUIT = request.CUIT,
            Email = request.Email, Password = request.Password, Rol = request.Rol,
            FechaIngreso = request.FechaIngreso,
            Telefono = request.Rol == Usuario.RolCliente ? Normalizacion.Opcional(request.Telefono) : request.Telefono,
            Direccion = request.Rol == Usuario.RolCliente ? Normalizacion.Opcional(request.Direccion) : request.Direccion
        }, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = usuario.Id }, usuario);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UsuarioUpdateRequest request)
    {
        var usuario = await _db.Usuarios.FindAsync(id);
        if (usuario is null)
            return NotFound();

        var email = request.Email;
        await _db.ExigirEmailLibreAsync(email, "Ya existe un usuario con ese email.", id);

        if (usuario.Rol == Usuario.RolCliente)
            await CuitUnico.ValidarClienteAsync(_db, request.CUIT, id);

        usuario.Nombre = request.Nombre.Trim();
        usuario.Apellido = request.Apellido.Trim();
        usuario.CUIT = request.CUIT.Trim();
        usuario.Email = email;
        if (request.Password is not null)
        {
            AuthService.ValidarPassword(request.Password);
            usuario.PasswordHash = _passwordHasher.HashPassword(usuario, request.Password);
        }

        await _db.GuardarOConflictoAsync("Ya existe una cuenta con esos datos únicos.");
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await _auth.EliminarUsuarioAsync(id, User.ObtenerUsuarioId(), cancellationToken);
        return NoContent();
    }
}
