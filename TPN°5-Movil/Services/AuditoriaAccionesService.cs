using System.Security.Claims;
using TPN_5_MOVIL.Data;
using TPN_5_MOVIL.Entities;
using TPN_5_MOVIL.Security;

namespace TPN_5_MOVIL.Services;

public sealed class AuditoriaAccionesService(AppDbContext db, IHttpContextAccessor httpContextAccessor) : IAuditoriaAccionesService
{
    public void Registrar(string accion, string entidad, string? entidadId, string? observacion = null)
    {
        var context = httpContextAccessor.HttpContext;
        var usuarioId = ObtenerUsuarioId(context);
        var usuarioEmail = context?.User?.FindFirstValue(ClaimTypes.Email) ?? context?.User?.FindFirstValue("email");
        var ip = context?.Connection?.RemoteIpAddress?.ToString();

        db.AuditoriaAcciones.Add(new AuditoriaAccion
        {
            UsuarioId = usuarioId,
            UsuarioEmail = string.IsNullOrWhiteSpace(usuarioEmail) ? null : usuarioEmail.Trim(),
            Accion = accion.Trim(),
            Entidad = entidad.Trim(),
            EntidadId = string.IsNullOrWhiteSpace(entidadId) ? null : entidadId.Trim(),
            FechaUtc = DateTime.UtcNow,
            Ip = string.IsNullOrWhiteSpace(ip) ? null : ip,
            Observacion = string.IsNullOrWhiteSpace(observacion) ? null : observacion.Trim()
        });
    }

    public Task RegistrarAsync(string accion, string entidad, string? entidadId, string? observacion = null, CancellationToken ct = default)
    {
        Registrar(accion, entidad, entidadId, observacion);
        return Task.CompletedTask;
    }

    private static int ObtenerUsuarioId(HttpContext? context)
    {
        if (context is null) throw new InvalidOperationException("No hay contexto HTTP para auditar la acción.");
        if (!context.User.Identity?.IsAuthenticated == true) throw new InvalidOperationException("No hay usuario autenticado para auditar la acción.");
        var id = UsuarioAutenticado.ObtenerUsuarioId(context.User);
        if (id <= 0) throw new InvalidOperationException("UsuarioId inválido para auditoría.");
        return id;
    }
}
