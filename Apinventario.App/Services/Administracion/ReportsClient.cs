using Apinventario.App.Models.Administracion;
using Apinventario.App.Models.Common;
using Apinventario.App.Services.Infrastructure;

namespace Apinventario.App.Services.Administracion;

public sealed class ReportsClient(ApiClient api)
{
    public Task<DownloadedFile> DownloadAsync(ReportForm form,CancellationToken ct=default)
    {
        Validate(form);
        var query=form.Tipo=="stock" ? "" : "?"+ApiClient.Query(("desde",form.Desde?.ToString("yyyy-MM-dd")),("hasta",form.Hasta?.ToString("yyyy-MM-dd")));
        return api.DownloadAsync($"api/reportes/{form.Tipo}"+query,ct);
    }
    public Task<SendReportResult> SendAsync(ReportForm form,CancellationToken ct=default)
    {
        Validate(form);
        if(string.IsNullOrWhiteSpace(form.Destinatario) || !new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(form.Destinatario) || form.Destinatario.Length>150)
            throw new ApiFailure(400,"Ingresá un destinatario válido, de hasta 150 caracteres.");
        return api.SendJsonAsync<SendReportResult>(HttpMethod.Post,$"api/reportes/{form.Tipo}/enviar",new {destinatario=form.Destinatario.Trim(),desde=form.Tipo!="stock"&&form.Desde.HasValue ? DateOnly.FromDateTime(form.Desde.Value):(DateOnly?)null,hasta=form.Tipo!="stock"&&form.Hasta.HasValue ? DateOnly.FromDateTime(form.Hasta.Value):(DateOnly?)null},ct);
    }
    private static void Validate(ReportForm form)
    {
        if(form.Tipo is not ("stock" or "ingresos" or "salidas"))throw new ApiFailure(400,"Tipo de reporte inválido.");
        if(form.Tipo!="stock" && (form.Desde>form.Hasta || form.Hasta?.Date==DateTime.MaxValue.Date))throw new ApiFailure(400,"Revisá el rango de fechas.");
    }
}
public sealed record SendReportResult(bool Enviado,string Mensaje);
