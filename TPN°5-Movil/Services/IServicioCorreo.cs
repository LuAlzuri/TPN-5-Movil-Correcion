namespace TPN_5_MOVIL.Services;

public interface IServicioCorreo
{
    Task EnviarReportePdfAsync(string destinatario, string asunto, byte[] adjuntoPdf, string nombreAdjunto, CancellationToken ct);
}
