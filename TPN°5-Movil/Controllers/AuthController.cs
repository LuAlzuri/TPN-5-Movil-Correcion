using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using TPN_5_MOVIL.Data;
using TPN_5_MOVIL.DTOs;
using TPN_5_MOVIL.Entities;
using TPN_5_MOVIL.Services;
using TPN_5_MOVIL.Security;

namespace TPN_5_MOVIL.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;
    private readonly AppDbContext _db;

    public AuthController(AuthService authService, AppDbContext db)
    {
        _authService = authService;
        _db = db;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting(LoginRateLimitSettings.PolicyName)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _authService.LoginAsync(request, cancellationToken));
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<UsuarioDto>> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        if (request.Rol != Usuario.RolCliente && !User.IsInRole(Usuario.RolAdministrador))
            return StatusCode(StatusCodes.Status403Forbidden,
                new { error = "El registro público solo permite crear clientes." });

        var usuario = await _authService.RegisterAsync(request, cancellationToken);
        return CreatedAtAction(nameof(UsuariosController.GetById), "Usuarios", new { id = usuario.Id }, usuario);
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<UsuarioDto>> Me()
    {
        var id = User.ObtenerUsuarioId();

        return await _db.Usuarios
            .AsNoTracking()
            .Where(u => u.Id == id)
            .Select(u => new UsuarioDto(u.Id, u.Nombre, u.Apellido, u.CUIT, u.Email, u.Rol, u.Eliminado))
            .FirstOrDefaultAsync() is var usuario && usuario is not null
            ? Ok(usuario)
            : NotFound();
    }
}
