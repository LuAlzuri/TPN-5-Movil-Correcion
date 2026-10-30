using System.ComponentModel.DataAnnotations;
using Apinventario.App.Models.Common;

namespace Apinventario.App.Models.Administracion;

public sealed class UserForm
{
    [Required, MaxLength(150)] public string Nombre { get; set; } = "";
    [Required, MaxLength(150)] public string Apellido { get; set; } = "";
    [Required, MaxLength(20)] public string CUIT { get; set; } = "";
    [Required, EmailAddress, MaxLength(150)] public string Email { get; set; } = "";
    public string Password { get; set; } = "";
    public string Rol { get; set; } = "Cliente";
    public DateTime? FechaIngreso { get; set; }
    [MaxLength(50)] public string? Telefono { get; set; }
    [MaxLength(300)] public string? Direccion { get; set; }
}
public sealed class CategoryForm
{
    [Required, MaxLength(100)] public string Nombre { get; set; } = "";
    [MaxLength(1000)] public string? Descripcion { get; set; }
}
public sealed record Category(int Id, string Nombre, string? Descripcion, DateTime FechaCreacion);
public sealed record EmployeeProfile(int Id, string Nombre, string Apellido, string CUIT, string Email, DateOnly FechaIngreso);
public sealed record ClientProfile(int Id, string Nombre, string Apellido, string CUIT, string Email, string? Telefono, string? Direccion);
public sealed record AuditAction(long Id, int UsuarioId, string? UsuarioEmail, string Accion, string Entidad,
    string? EntidadId, DateTime FechaUtc, string? Ip, string? Observacion);
public sealed class ActionPage
{
    public List<AuditAction> Items { get; set; } = [];
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)Total / PageSize) : 0;
}
public sealed class ReportForm : IValidatableObject
{
    [Required] public string Tipo { get; set; } = "stock";
    public DateTime? Desde { get; set; }
    public DateTime? Hasta { get; set; }
    [EmailAddress, MaxLength(150)] public string? Destinatario { get; set; }
    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (Tipo != "stock" && Desde > Hasta) yield return new("Desde no puede ser posterior a Hasta.", [nameof(Desde), nameof(Hasta)]);
        if (Hasta?.Date == DateTime.MaxValue.Date) yield return new("Elegí una fecha anterior a 9999-12-31.", [nameof(Hasta)]);
    }
}
