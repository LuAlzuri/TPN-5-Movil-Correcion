using System.Net.Http.Headers;
using Apinventario.App.Models.Comercio;
using Apinventario.App.Models.Common;
using Apinventario.App.Services.Infrastructure;

namespace Apinventario.App.Services.Comercio;

public sealed class CatalogoClient(ApiClient api)
{
    public Task<PageResult<ProductoDto>> ProductosAsync(int page, int pageSize, string? busqueda = null,
        int? categoriaId = null, int? proveedorId = null, CancellationToken ct = default) =>
        api.SendJsonAsync<PageResult<ProductoDto>>(HttpMethod.Get, "api/productos?" + ApiClient.Query(
            ("page", page), ("pageSize", pageSize), ("busqueda", busqueda),
            ("categoriaId", categoriaId), ("proveedorId", proveedorId)), ct: ct);

    public Task<ProductoDto> ProductoAsync(int id, CancellationToken ct = default) =>
        api.SendJsonAsync<ProductoDto>(HttpMethod.Get, $"api/productos/{id}", ct: ct);

    public Task<List<CategoriaCatalogoDto>> CategoriasAsync(CancellationToken ct = default) =>
        api.SendJsonAsync<List<CategoriaCatalogoDto>>(HttpMethod.Get, "api/categorias/catalogo", ct: ct);

    public Task<PageResult<MovimientoDto>> MovimientosAsync(int productoId, int page, int pageSize = 10,
        CancellationToken ct = default) =>
        api.SendJsonAsync<PageResult<MovimientoDto>>(HttpMethod.Get,
            $"api/productos/{productoId}/movimientos?" + ApiClient.Query(("page", page), ("pageSize", pageSize)), ct: ct);

    public Task<ProductoDto> CrearAsync(ProductoForm form, CancellationToken ct = default)
    {
        Validar(form);
        return api.SendJsonAsync<ProductoDto>(HttpMethod.Post, "api/productos", new
        {
            nombre = form.Nombre.Trim(),
            descripcion = string.IsNullOrWhiteSpace(form.Descripcion) ? null : form.Descripcion.Trim(),
            precio = form.Precio!.Value,
            categoriaId = form.CategoriaId!.Value
        }, ct);
    }

    public Task ActualizarAsync(int id, ProductoForm form, CancellationToken ct = default)
    {
        Validar(form);
        return api.SendNoContentAsync(HttpMethod.Put, $"api/productos/{id}", new
        {
            nombre = form.Nombre.Trim(),
            descripcion = string.IsNullOrWhiteSpace(form.Descripcion) ? null : form.Descripcion.Trim(),
            precio = form.Precio!.Value,
            categoriaId = form.CategoriaId!.Value
        }, ct);
    }

    public Task EliminarAsync(int id, CancellationToken ct = default) =>
        api.SendNoContentAsync(HttpMethod.Delete, $"api/productos/{id}", ct: ct);

    public Task<ImagenProductoDto> SubirImagenAsync(int id, string fileName, string contentType, Stream content,
        CancellationToken ct = default)
    {
        using var body = new MultipartFormDataContent();
        var file = new StreamContent(content);
        file.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        body.Add(file, "File", fileName);
        return api.SendMultipartAsync<ImagenProductoDto>($"api/productos/{id}/imagen", body, ct);
    }

    public static string ContentTypeFor(string fileName) => Path.GetExtension(fileName).ToLowerInvariant() switch
    {
        ".jpg" or ".jpeg" => "image/jpeg",
        ".png" => "image/png",
        ".webp" => "image/webp",
        _ => "application/octet-stream"
    };

    private static void Validar(ProductoForm form)
    {
        var errors = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        if (!System.ComponentModel.DataAnnotations.Validator.TryValidateObject(form,
            new System.ComponentModel.DataAnnotations.ValidationContext(form), errors, true))
            throw new ApiFailure(400, string.Join(" ", errors.Select(x => x.ErrorMessage)));
    }
}
