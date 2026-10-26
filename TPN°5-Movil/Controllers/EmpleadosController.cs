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
[Route("api/empleados")]
[Authorize(Roles = Usuario.RolAdministrador)]
public class EmpleadosController(AppDbContext db, AuthService auth) : ControllerBase
{
    private IQueryable<Empleado> EmpleadosValidos => db.Empleados
        .Where(e => e.Usuario.Rol == Usuario.RolEmpleado && e.Usuario.Cliente == null);

    [HttpGet]
    public async Task<ActionResult<PaginatedResult<EmpleadoResponse>>> GetAll(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 10,
        [FromQuery] string? busqueda = null)
    {
        if (page < 1 || pageSize < 1 || pageSize > 100 || (long)(page - 1) * pageSize > int.MaxValue)
            throw new ApiException("Página inválida: page debe ser positivo y pageSize debe estar entre 1 y 100.");

        var query = EmpleadosValidos.AsNoTracking();
        busqueda = busqueda?.Trim();
        if (!string.IsNullOrWhiteSpace(busqueda))
            query = query.Where(e => (e.Usuario.Nombre + " " + e.Usuario.Apellido).Contains(busqueda)
                || e.Usuario.Email.Contains(busqueda) || e.Usuario.CUIT.Contains(busqueda));

        return Ok(await PaginatedResult<EmpleadoResponse>.CreateAsync(
            query.OrderBy(e => e.Usuario.Apellido).ThenBy(e => e.Usuario.Nombre).ThenBy(e => e.Id)
                .Select(e => new EmpleadoResponse(e.Id, e.Usuario.Nombre, e.Usuario.Apellido,
                    e.Usuario.CUIT, e.Usuario.Email, e.FechaIngreso)), page, pageSize));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<EmpleadoResponse>> GetById(int id)
    {
        var response = await EmpleadosValidos.AsNoTracking().Where(e => e.Id == id)
            .Select(e => new EmpleadoResponse(e.Id, e.Usuario.Nombre, e.Usuario.Apellido,
                e.Usuario.CUIT, e.Usuario.Email, e.FechaIngreso)).FirstOrDefaultAsync();
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

        var email = request.Email.Trim().ToLowerInvariant();
        var comparison = email.ToUpperInvariant();
        if (await db.Usuarios.IgnoreQueryFilters().AnyAsync(u => u.Id != id && u.Email.ToUpper() == comparison))
            throw new ApiException("Ya existe una cuenta con ese email.", StatusCodes.Status409Conflict);

        empleado.Usuario.Nombre = request.Nombre.Trim();
        empleado.Usuario.Apellido = request.Apellido.Trim();
        empleado.Usuario.CUIT = request.CUIT.Trim();
        empleado.Usuario.Email = email;
        empleado.FechaIngreso = request.FechaIngreso!.Value;
        try
        {
            // SaveChanges wraps the account and profile updates in a single transaction.
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
