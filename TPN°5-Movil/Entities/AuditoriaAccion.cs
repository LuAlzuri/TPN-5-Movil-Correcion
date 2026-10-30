using System.ComponentModel.DataAnnotations;

namespace TPN_5_MOVIL.Entities;

public class AuditoriaAccion
{
    public long Id { get; set; }

    [Required]
    public int UsuarioId { get; set; }

    [MaxLength(150)]
    public string? UsuarioEmail { get; set; }

    [Required]
    [MaxLength(50)]
    public string Accion { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Entidad { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? EntidadId { get; set; }

    [Required]
    public DateTime FechaUtc { get; set; } = DateTime.UtcNow;

    [MaxLength(45)]
    public string? Ip { get; set; }

    [MaxLength(250)]
    public string? Observacion { get; set; }
}
