using Apinventario.App.Models.Comercio;
using Apinventario.App.Models.Common;
using Apinventario.App.Services.Infrastructure;

namespace Apinventario.App.Services.Comercio;

public sealed class ClientesClient(ApiClient api)
{
    public Task<PageResult<ClienteDto>> ListarAsync(int page, int pageSize = 20, string? busqueda = null,
        CancellationToken ct = default) =>
        api.SendJsonAsync<PageResult<ClienteDto>>(HttpMethod.Get, "api/clientes?" + ApiClient.Query(
            ("page", page), ("pageSize", pageSize), ("busqueda", busqueda)), ct: ct);

    public Task<List<ClienteDto>> CatalogoAsync(CancellationToken ct = default) =>
        api.SendJsonAsync<List<ClienteDto>>(HttpMethod.Get, "api/clientes/catalogo", ct: ct);

    public Task<ClienteDto> ClienteAsync(int id, CancellationToken ct = default) =>
        api.SendJsonAsync<ClienteDto>(HttpMethod.Get, $"api/clientes/{id}", ct: ct);
}
