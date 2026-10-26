using System.Globalization;
using System.Security.Claims;
using TPN_5_MOVIL.Entities;
using TPN_5_MOVIL.Exceptions;

namespace TPN_5_MOVIL.Security;

public static class UsuarioAutenticado
{
    public static int ObtenerUsuarioId(this ClaimsPrincipal principal)
    {
        var value = principal.FindFirst("sub")?.Value;
        if (principal.Identity?.IsAuthenticated != true ||
            !int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var id) || id <= 0)
            throw new ApiException("Identidad no válida.", StatusCodes.Status401Unauthorized);
        return id;
    }

    public static int ObtenerClienteId(this ClaimsPrincipal principal)
    {
        if (!principal.IsInRole(Usuario.RolCliente))
            throw new ApiException("Se requiere un perfil de cliente.", StatusCodes.Status403Forbidden);
        return principal.ObtenerUsuarioId();
    }

    public static int ObtenerEmpleadoId(this ClaimsPrincipal principal)
    {
        if (!principal.IsInRole(Usuario.RolEmpleado))
            throw new ApiException("Se requiere un perfil de empleado.", StatusCodes.Status403Forbidden);
        return principal.ObtenerUsuarioId();
    }
}
