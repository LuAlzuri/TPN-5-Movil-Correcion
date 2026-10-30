using Apinventario.App.Models.Comercio;
using Apinventario.App.Models.Common;
using Apinventario.App.Services.Infrastructure;

namespace Apinventario.App.Services.Comercio;

public sealed class CarritoClient(ApiClient api)
{
    public Task<PageResult<SalidaDto>> ListarAsync(int page, int pageSize = 20, int? productoId = null,
        int? clienteId = null, DateTime? desde = null, DateTime? hasta = null, CancellationToken ct = default) =>
        api.SendJsonAsync<PageResult<SalidaDto>>(HttpMethod.Get, "api/salidas?" + ApiClient.Query(
            ("page", page), ("pageSize", pageSize), ("productoId", productoId), ("clienteId", clienteId),
            ("desde", ComercioDates.Start(desde)), ("hasta", ComercioDates.End(hasta))), ct: ct);

    public Task<SalidaDto> SalidaAsync(int id, CancellationToken ct = default) =>
        api.SendJsonAsync<SalidaDto>(HttpMethod.Get, $"api/salidas/{id}", ct: ct);

    public Task<DetalleSalidaDto> DetalleAsync(int id, int detalleId, CancellationToken ct = default) =>
        api.SendJsonAsync<DetalleSalidaDto>(HttpMethod.Get, $"api/salidas/{id}/detalles/{detalleId}", ct: ct);

    public Task<SalidaDto> CrearAsync(string? observaciones = null, CancellationToken ct = default) =>
        api.SendJsonAsync<SalidaDto>(HttpMethod.Post, "api/salidas", new
        {
            observaciones = string.IsNullOrWhiteSpace(observaciones) ? null : observaciones.Trim()
        }, ct);

    public Task<DetalleSalidaDto> AgregarDetalleAsync(int id, int productoId, int cantidad,
        CancellationToken ct = default) =>
        api.SendJsonAsync<DetalleSalidaDto>(HttpMethod.Post, $"api/salidas/{id}/detalles",
            new { productoId, cantidad }, ct);

    public async Task<DetalleSalidaDto?> EditarDetalleAsync(int id, int detalleId, int cantidad,
        CancellationToken ct = default)
    {
        if (cantidad == 0)
        {
            await api.SendNoContentAsync(HttpMethod.Put, $"api/salidas/{id}/detalles/{detalleId}",
                new { cantidad }, ct);
            return null;
        }
        return await api.SendJsonAsync<DetalleSalidaDto>(HttpMethod.Put,
            $"api/salidas/{id}/detalles/{detalleId}", new { cantidad }, ct);
    }

    public Task QuitarDetalleAsync(int id, int detalleId, CancellationToken ct = default) =>
        api.SendNoContentAsync(HttpMethod.Delete, $"api/salidas/{id}/detalles/{detalleId}", ct: ct);

    public Task<SalidaDto> ConfirmarClienteAsync(int id, string? medioPago = null, CancellationToken ct = default) =>
        api.SendJsonAsync<SalidaDto>(HttpMethod.Post, $"api/salidas/{id}/confirmar-cliente", new { medioPago }, ct);

    public Task<DownloadedFile> ComprobanteAsync(int id, CancellationToken ct = default) =>
        api.DownloadAsync($"api/salidas/{id}/comprobante", ct);

    public Task<SalidaDto> ConfirmarAsync(int id, CancellationToken ct = default) =>
        api.SendJsonAsync<SalidaDto>(HttpMethod.Post, $"api/salidas/{id}/confirmar", ct: ct);

    public Task EliminarAsync(int id, CancellationToken ct = default) =>
        api.SendNoContentAsync(HttpMethod.Delete, $"api/salidas/{id}", ct: ct);

    public async Task<List<SalidaDto>> ListarBorradoresAsync(int pageSize = 100, CancellationToken ct = default)
    {
        var borradores = new List<SalidaDto>();
        for (var page = 1; ; page++)
        {
            var result = await ListarAsync(page, pageSize, ct: ct);
            borradores.AddRange(result.Items.Where(PedidoEstado.EsBorradorActivo));
            if (!result.HasNext) break;
        }
        return borradores.OrderByDescending(x => x.Fecha).ThenByDescending(x => x.Id).ToList();
    }

    public async Task<SalidaDto> AsegurarBorradorAsync(int? preferido, CancellationToken ct = default)
    {
        if (preferido.HasValue)
        {
            try
            {
                var actual = await SalidaAsync(preferido.Value, ct);
                if (PedidoEstado.EsBorradorActivo(actual)) return actual;
            }
            catch (ApiFailure ex) when (ex.StatusCode == 404) { }
        }
        var borradores = await ListarBorradoresAsync(ct: ct);
        return borradores.FirstOrDefault() ?? await CrearAsync(ct: ct);
    }
}
