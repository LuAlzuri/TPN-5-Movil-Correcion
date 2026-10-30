using System.Text.Json;
using Apinventario.App.Models.Administracion;
using Apinventario.App.Models.Common;
using Apinventario.App.Services.Infrastructure;

namespace Apinventario.App.Services.Administracion;

public sealed class AuditClient(ApiClient api)
{
    public Task<ActionPage> ActionsAsync(int page,int? user,string? entity,string? action,DateTime? from,DateTime? to,CancellationToken ct=default) =>
        api.SendJsonAsync<ActionPage>(HttpMethod.Get,"api/auditoria/acciones?"+ApiClient.Query(("page",page),("pageSize",20),("usuarioId",user),("entidad",entity),("accion",action),("desde",Start(from)),("hasta",End(to))),ct:ct);
    public Task<AuditAction> ActionAsync(long id,CancellationToken ct=default) => api.SendJsonAsync<AuditAction>(HttpMethod.Get,$"api/auditoria/acciones/{id}",ct:ct);
    public Task<PageResult<JsonElement>> HistoryAsync(string resource,int page,IEnumerable<(string Key,object? Value)> filters,CancellationToken ct=default)
    {
        CheckResource(resource);
        return api.SendJsonAsync<PageResult<JsonElement>>(HttpMethod.Get,$"api/auditoria/{resource}?"+ApiClient.Query(filters.Concat([("page",(object?)page),("pageSize",20)]).ToArray()),ct:ct);
    }
    public Task<JsonElement> DetailAsync(string resource,int id,CancellationToken ct=default)
    { CheckResource(resource);return api.SendJsonAsync<JsonElement>(HttpMethod.Get,$"api/auditoria/{resource}/{id}",ct:ct); }
    public Task<PageResult<JsonElement>> MovementsAsync(int productId,int page,string? type,DateTime? from,DateTime? to,CancellationToken ct=default) =>
        api.SendJsonAsync<PageResult<JsonElement>>(HttpMethod.Get,$"api/auditoria/productos/{productId}/movimientos?"+ApiClient.Query(("page",page),("pageSize",20),("tipo",type),("desde",Start(from)),("hasta",End(to))),ct:ct);
    private static void CheckResource(string value)
    { if(value is not ("usuarios" or "productos" or "ingresos" or "salidas"))throw new ArgumentException("Recurso de auditoría no admitido."); }
    // Date filters in this UI are explicitly business days in UTC-03.
    public static string? Start(DateTime? date) => date.HasValue ? new DateTimeOffset(date.Value.Date,TimeSpan.FromHours(-3)).UtcDateTime.ToString("O") : null;
    public static string? End(DateTime? date) => date.HasValue && date.Value.Date < DateTime.MaxValue.Date ? new DateTimeOffset(date.Value.Date.AddDays(1).AddTicks(-1),TimeSpan.FromHours(-3)).UtcDateTime.ToString("O") : null;
}
