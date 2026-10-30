using Apinventario.App.Models.Comercio;
using Apinventario.App.Models.Common;
using Apinventario.App.Services.Infrastructure;

namespace Apinventario.App.Services.Comercio;

public sealed class IngresosClient(ApiClient api)
{
    public Task<PageResult<IngresoDto>> ListarAsync(int page, int pageSize = 20, int? productoId = null,
        int? proveedorId = null, DateTime? desde = null, DateTime? hasta = null, CancellationToken ct = default) =>
        api.SendJsonAsync<PageResult<IngresoDto>>(HttpMethod.Get, "api/ingresos?" + ApiClient.Query(
            ("page", page), ("pageSize", pageSize), ("productoId", productoId), ("proveedorId", proveedorId),
            ("desde", ComercioDates.Start(desde)), ("hasta", ComercioDates.End(hasta))), ct: ct);

    public Task<IngresoDto> IngresoAsync(int id, CancellationToken ct = default) =>
        api.SendJsonAsync<IngresoDto>(HttpMethod.Get, $"api/ingresos/{id}", ct: ct);

    public Task<DetalleIngresoDto> DetalleAsync(int id, int detalleId, CancellationToken ct = default) =>
        api.SendJsonAsync<DetalleIngresoDto>(HttpMethod.Get, $"api/ingresos/{id}/detalles/{detalleId}", ct: ct);

    public Task<IngresoDto> CrearAsync(IngresoForm form, CancellationToken ct = default) =>
        api.SendJsonAsync<IngresoDto>(HttpMethod.Post, "api/ingresos", new
        {
            proveedorId = form.ProveedorId!.Value,
            observaciones = string.IsNullOrWhiteSpace(form.Observaciones) ? null : form.Observaciones.Trim()
        }, ct);

    public Task<DetalleIngresoDto> AgregarDetalleAsync(int id, DetalleIngresoForm form, CancellationToken ct = default) =>
        api.SendJsonAsync<DetalleIngresoDto>(HttpMethod.Post, $"api/ingresos/{id}/detalles", new
        {
            productoId = form.ProductoId!.Value,
            cantidad = form.Cantidad!.Value,
            descAplicado = form.DescAplicado
        }, ct);

    public Task<DetalleIngresoDto> EditarDetalleAsync(int id, int detalleId, DetalleIngresoForm form,
        CancellationToken ct = default) =>
        api.SendJsonAsync<DetalleIngresoDto>(HttpMethod.Put, $"api/ingresos/{id}/detalles/{detalleId}", new
        {
            cantidad = form.Cantidad!.Value,
            descAplicado = form.DescAplicado
        }, ct);

    public Task QuitarDetalleAsync(int id, int detalleId, CancellationToken ct = default) =>
        api.SendNoContentAsync(HttpMethod.Delete, $"api/ingresos/{id}/detalles/{detalleId}", ct: ct);

    public Task<IngresoDto> ConfirmarAsync(int id, CancellationToken ct = default) =>
        api.SendJsonAsync<IngresoDto>(HttpMethod.Post, $"api/ingresos/{id}/confirmar", ct: ct);

    public Task EliminarAsync(int id, CancellationToken ct = default) =>
        api.SendNoContentAsync(HttpMethod.Delete, $"api/ingresos/{id}", ct: ct);
}
