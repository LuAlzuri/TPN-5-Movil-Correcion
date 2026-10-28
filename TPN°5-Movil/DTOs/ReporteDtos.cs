using System.ComponentModel.DataAnnotations;

namespace TPN_5_MOVIL.DTOs;

public record ReporteStockFila(int Id, string Nombre, string Categoria, decimal Precio, int Stock);
public record ReporteMovimientoFila(int Id, DateTime FechaConfirmacionUtc, string Contraparte, string Confirmador, decimal Total);

// Presentation uses an explicit fixed UTC-03:00 offset, independently of the host.
public record ReporteContexto(DateTimeOffset GeneradoUtc, DateOnly? Desde = null, DateOnly? Hasta = null)
{
    public static readonly TimeSpan Offset = TimeSpan.FromHours(-3);
    public static DateTimeOffset Presentar(DateTime utc) =>
        new DateTimeOffset(DateTime.SpecifyKind(utc, DateTimeKind.Utc)).ToOffset(Offset);
}

public sealed class ReporteFechasFiltro : IValidatableObject
{
    public DateOnly? Desde { get; init; }
    public DateOnly? Hasta { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (Desde > Hasta)
            yield return new ValidationResult("Desde no puede ser posterior a Hasta.", [nameof(Desde), nameof(Hasta)]);
        if (Hasta == DateOnly.MaxValue)
            yield return new ValidationResult("Hasta debe ser anterior a 9999-12-31 para incluir el día completo.", [nameof(Hasta)]);
    }

    public DateTime? ObtenerDesdeUtc() => Desde.HasValue ? InicioUtc(Desde.Value) : null;
    public DateTime? ObtenerHastaExclusivoUtc() => Hasta.HasValue ? InicioUtc(Hasta.Value.AddDays(1)) : null;
    private static DateTime InicioUtc(DateOnly date) =>
        new DateTimeOffset(date.ToDateTime(TimeOnly.MinValue), ReporteContexto.Offset).UtcDateTime;
}
