using Apinventario.App.Models.Comercio;
using Apinventario.App.Models.Common;
using Apinventario.App.Services.Infrastructure;

namespace Apinventario.App.Services.Comercio;

public sealed class ProveedoresClient(ApiClient api)
{
    public Task<PageResult<ProveedorDto>> ListarAsync(int page, int pageSize = 20, string? busqueda = null,
        CancellationToken ct = default) =>
        api.SendJsonAsync<PageResult<ProveedorDto>>(HttpMethod.Get, "api/proveedores?" + ApiClient.Query(
            ("page", page), ("pageSize", pageSize), ("busqueda", busqueda)), ct: ct);

    public Task<List<ProveedorDto>> CatalogoAsync(CancellationToken ct = default) =>
        api.SendJsonAsync<List<ProveedorDto>>(HttpMethod.Get, "api/proveedores/catalogo", ct: ct);

    public Task<ProveedorDto> ProveedorAsync(int id, CancellationToken ct = default) =>
        api.SendJsonAsync<ProveedorDto>(HttpMethod.Get, $"api/proveedores/{id}", ct: ct);

    public Task<ProveedorDto> CrearAsync(ProveedorForm form, CancellationToken ct = default) =>
        api.SendJsonAsync<ProveedorDto>(HttpMethod.Post, "api/proveedores", Form(form), ct);

    public Task ActualizarAsync(int id, ProveedorForm form, CancellationToken ct = default) =>
        api.SendNoContentAsync(HttpMethod.Put, $"api/proveedores/{id}", Form(form), ct);

    public Task EliminarAsync(int id, CancellationToken ct = default) =>
        api.SendNoContentAsync(HttpMethod.Delete, $"api/proveedores/{id}", ct: ct);

    private static object Form(ProveedorForm form) => new
    {
        nombre = form.Nombre.Trim(),
        cuit = form.CUIT.Trim(),
        email = string.IsNullOrWhiteSpace(form.Email) ? null : form.Email.Trim(),
        telefono = string.IsNullOrWhiteSpace(form.Telefono) ? null : form.Telefono.Trim(),
        direccion = string.IsNullOrWhiteSpace(form.Direccion) ? null : form.Direccion.Trim()
    };
}
