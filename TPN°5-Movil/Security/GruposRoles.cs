using TPN_5_MOVIL.Entities;

namespace TPN_5_MOVIL.Security;

public static class GruposRoles
{
    public const string Personal = Usuario.RolAdministrador + "," + Usuario.RolEmpleado;
    public const string Todos = Personal + "," + Usuario.RolCliente;
}
