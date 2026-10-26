namespace TPN_5_MOVIL.Entities;

public class Usuario
{
    public const string RolAdministrador = "Administrador";
    public const string RolEmpleado = "Empleado";
    public const string RolCliente = "Cliente";

    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string CUIT { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Rol { get; set; } = RolCliente;
    public bool Eliminado { get; set; }
    public Empleado? Empleado { get; set; }
    public Cliente? Cliente { get; set; }
}
