using System.ComponentModel.DataAnnotations;

namespace TPN_5_MOVIL.DTOs;

public sealed class ReporteEnviarRequest : IValidatableObject
{
    [Required(ErrorMessage = "El destinatario es obligatorio.")]
    [EmailAddress(ErrorMessage = "El destinatario debe ser un correo electrónico válido.")]
    [MaxLength(150)]
    public string Destinatario { get; set; } = string.Empty;

    public DateOnly? Desde { get; set; }

    public DateOnly? Hasta { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (Desde.HasValue && Hasta.HasValue && Desde.Value > Hasta.Value)
            yield return new ValidationResult("Desde no puede ser posterior a Hasta.", [nameof(Desde), nameof(Hasta)]);

        if (Hasta.HasValue && Hasta.Value == DateOnly.MaxValue)
            yield return new ValidationResult("Hasta debe ser anterior a 9999-12-31 para incluir el día completo.", [nameof(Hasta)]);
    }
}

public sealed record ReporteEnviarResponse(bool Enviado, string Mensaje);
