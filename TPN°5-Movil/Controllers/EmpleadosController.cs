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
[Route("api/empleados")]
[Authorize(Roles = Usuario.RolAdministrador)]
public class EmpleadosController(AppDbContext db, AuthService auth) : ControllerBase
{
    private IQueryable<Empleado> EmpleadosValidos => db.Empleados
        .Where(e => e.Usuario.Rol == Usuario.RolEmpleado && e.Usuario.Cliente == null);

    private static IQueryable<EmpleadoResponse> Respuestas(IQueryable<Empleado> query) =>
        query.Select(e => new EmpleadoResponse(e.Id, e.Usuario.Nombre, e.Usuario.Apellido,
            e.Usuario.CUIT, e.Usuario.Email, e.FechaIngreso));

    [HttpGet]
    public async Task<ActionResult<PaginatedResult<EmpleadoResponse>>> GetAll(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 10,
        [FromQuery] string? busqueda = null)
    {
        Paginacion.Validar(page, pageSize);

        var query = EmpleadosValidos.AsNoTracking();
        busqueda = busqueda?.Trim();
        if (!string.IsNullOrWhiteSpace(busqueda))
            query = query.Where(e => (e.Usuario.Nombre + " " + e.Usuario.Apellido).Contains(busqueda)
                || e.Usuario.Email.Contains(busqueda) || e.Usuario.CUIT.Contains(busqueda));

        return Ok(await PaginatedResult<EmpleadoResponse>.CreateAsync(
            Respuestas(query.OrderBy(e => e.Usuario.Apellido).ThenBy(e => e.Usuario.Nombre).ThenBy(e => e.Id)),
            page, pageSize));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<EmpleadoResponse>> GetById(int id)
    {
        var response = await Respuestas(EmpleadosValidos.AsNoTracking().Where(e => e.Id == id))
            .FirstOrDefaultAsync();
        return response is null ? NotFound() : Ok(response);
    }

    [HttpPost]
    public async Task<ActionResult<EmpleadoResponse>> Create(EmpleadoCreateRequest request)
    {
        ValidarFecha(request.FechaIngreso);
        var user = await auth.RegisterAsync(new RegisterRequest
        {
            Nombre = request.Nombre, Apellido = request.Apellido, CUIT = request.CUIT,
            Email = request.Email, Password = request.Password, Rol = Usuario.RolEmpleado,
            FechaIngreso = request.FechaIngreso
        });
        return CreatedAtAction(nameof(GetById), new { id = user.Id },
            new EmpleadoResponse(user.Id, user.Nombre, user.Apellido, user.CUIT,
                user.Email, request.FechaIngreso!.Value));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, EmpleadoUpdateRequest request)
    {
        ValidarFecha(request.FechaIngreso);
        var empleado = await EmpleadosValidos.Include(e => e.Usuario).FirstOrDefaultAsync(e => e.Id == id);
        if (empleado is null) return NotFound();

        var email = Normalizacion.Email(request.Email);
        await db.ExigirEmailLibreAsync(email, "Ya existe una cuenta con ese email.", id);

        AuthService.AsignarDatos(empleado.Usuario, request.Nombre, request.Apellido, request.CUIT, email);
        empleado.FechaIngreso = request.FechaIngreso!.Value;
        await db.GuardarOConflictoAsync("Ya existe una cuenta con esos datos únicos.");
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var empleado = await EmpleadosValidos.Include(e => e.Usuario).FirstOrDefaultAsync(e => e.Id == id);
        if (empleado is null) return NotFound();
        empleado.Usuario.Eliminado = true;
        await db.SaveChangesAsync();
        return NoContent();
    }

    private static void ValidarFecha(DateOnly? fecha)
    {
        if (fecha is null || fecha == DateOnly.MinValue)
            throw new ApiException("Debe indicar una FechaIngreso válida.");
    }
}
