using System.Net;
using System.Net.Mail;
using TPN_5_MOVIL.Exceptions;
using TPN_5_MOVIL.Security;

namespace TPN_5_MOVIL.Services;

public sealed class ServicioCorreoSmtp(SmtpSettings settings, ILogger<ServicioCorreoSmtp> logger) : IServicioCorreo
{
    public async Task EnviarReportePdfAsync(string destinatario, string asunto, byte[] adjuntoPdf, string nombreAdjunto, CancellationToken ct)
    {
        if (!settings.EstaConfigurado())
            throw new ApiException("El envío de reportes por correo no está configurado o habilitado.", StatusCodes.Status503ServiceUnavailable);

        if (string.IsNullOrWhiteSpace(destinatario))
            throw new ApiException("El destinatario es obligatorio.", StatusCodes.Status400BadRequest);

        if (adjuntoPdf is null || adjuntoPdf.Length == 0)
            throw new ApiException("No se pudo generar el archivo PDF para enviar.", StatusCodes.Status500InternalServerError);

        try
        {
            using var client = new SmtpClient(settings.Host, settings.Puerto)
            {
                EnableSsl = settings.UsarSsl,
                Timeout = settings.TimeoutSegundos * 1000,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false
            };

            if (!string.IsNullOrWhiteSpace(settings.Usuario))
                client.Credentials = new NetworkCredential(settings.Usuario, settings.Contrasena ?? string.Empty);

            using var message = new MailMessage
            {
                From = new MailAddress(settings.Remitente),
                Subject = asunto,
                Body = "Se adjunta el reporte PDF solicitado.",
                IsBodyHtml = false
            };

            message.To.Add(new MailAddress(destinatario.Trim()));

            var stream = new MemoryStream(adjuntoPdf, writable: false);
            var attachment = new Attachment(stream, nombreAdjunto, "application/pdf");
            message.Attachments.Add(attachment);

            ct.ThrowIfCancellationRequested();
            // SmtpClient.SendMailAsync no acepta CancellationToken en todas las versiones; usar SendMailAsync con timeout configurado
            await client.SendMailAsync(message).ConfigureAwait(false);
        }
        catch (ApiException)
        {
            throw;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Fallo al enviar reporte por correo. No se exponen detalles al cliente.");
            throw new ApiException("No se pudo enviar el reporte por correo.", StatusCodes.Status502BadGateway);
        }
    }
}
