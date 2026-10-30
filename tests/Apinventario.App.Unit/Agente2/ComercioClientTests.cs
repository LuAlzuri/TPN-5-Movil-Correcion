using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Apinventario.App.Models.Comercio;
using Apinventario.App.Models.Common;
using Apinventario.App.Services.Comercio;
using Apinventario.App.Services.Infrastructure;
using Xunit;

namespace Apinventario.App.Unit.Agente2;

public sealed class ComercioClientTests
{
    private sealed class MemoryTokens : ITokenStore
    {
        public StoredSession? Value = new("private-test-token", DateTimeOffset.UtcNow.AddHours(1));
        public Task<StoredSession?> ReadAsync() => Task.FromResult(Value);
        public Task WriteAsync(StoredSession value) { Value = value; return Task.CompletedTask; }
        public void Clear() => Value = null;
    }

    private sealed class Handler(Func<HttpRequestMessage, Task<HttpResponseMessage>> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) => respond(request);
    }

    private static HttpResponseMessage Json(object body, HttpStatusCode code = HttpStatusCode.OK) =>
        new(code) { Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json") };

    private static object Sale(int id, bool client = false, bool employee = false, bool deleted = false) => new
    {
        id, clienteId = 4, usuarioConfirmacionId = (int?)null, fecha = "2026-10-29T10:00:00Z", observaciones = (string?)null,
        confirmarXCliente = client, confirmarXEmpleado = employee, fechaConfirmacionCliente = (DateTime?)null,
        fechaConfirmacionEmpleado = (DateTime?)null, detalles = Array.Empty<object>(), total = 0m, eliminado = deleted
    };

    private sealed class Fixture : IDisposable
    {
        public readonly string Directory = Path.Combine(Path.GetTempPath(), "apinventario-tests", Guid.NewGuid().ToString("N"));
        public readonly MemoryTokens Tokens = new();
        public readonly ApiConfig Config = new("https://api.example.test");
        public readonly ApiClient Api;
        private readonly HttpClient http;
        public Fixture(Func<HttpRequestMessage, Task<HttpResponseMessage>> response)
        {
            var logger = new AppErrorLogger(Directory);
            http = new(new Handler(response));
            Api = new(http, Config, Tokens, logger);
        }
        public void Dispose() => http.Dispose();
    }

    [Fact]
    public async Task CatalogSendsPaginationAndOnlySelectedFilters()
    {
        using var f = new Fixture(r =>
        {
            Assert.Equal("/api/productos", r.RequestUri!.AbsolutePath);
            Assert.Contains("page=2", r.RequestUri.Query);
            Assert.Contains("pageSize=12", r.RequestUri.Query);
            Assert.Contains("categoriaId=3", r.RequestUri.Query);
            Assert.DoesNotContain("busqueda", r.RequestUri.Query);
            Assert.DoesNotContain("proveedorId", r.RequestUri.Query);
            return Task.FromResult(Json(new
            {
                page = 2, pageSize = 12, totalCount = 13, totalPages = 2, hasNext = false, hasPrevious = true,
                items = new[] { new { id = 7, nombre = "Yerba", descripcion = (string?)null, precio = 1500m, stock = 4, imagenUrl = (string?)null, categoriaId = 3, categoriaNombre = "Almacén" } }
            }));
        });

        var page = await new CatalogoClient(f.Api).ProductosAsync(2, 12, categoriaId: 3);

        Assert.Equal(13, page.TotalCount);
        Assert.Equal("Yerba", page.Items[0].Nombre);
    }

    [Fact]
    public async Task ProductCreateSendsOnlyFieldsAcceptedByServer()
    {
        using var f = new Fixture(async r =>
        {
            Assert.Equal(HttpMethod.Post, r.Method);
            using var body = JsonDocument.Parse(await r.Content!.ReadAsStringAsync());
            Assert.Equal("Yerba", body.RootElement.GetProperty("nombre").GetString());
            Assert.Equal(1500m, body.RootElement.GetProperty("precio").GetDecimal());
            Assert.Equal(3, body.RootElement.GetProperty("categoriaId").GetInt32());
            Assert.False(body.RootElement.TryGetProperty("stock", out _));
            Assert.False(body.RootElement.TryGetProperty("imagenUrl", out _));
            return Json(new { id = 7, nombre = "Yerba", descripcion = (string?)null, precio = 1500m, stock = 0, imagenUrl = (string?)null, categoriaId = 3, categoriaNombre = "Almacén" });
        });

        await new CatalogoClient(f.Api).CrearAsync(new() { Nombre = " Yerba ", Precio = 1500m, CategoriaId = 3 });
    }

    [Fact]
    public async Task ImageUploadUsesFileFieldAndDeclaredContentType()
    {
        using var f = new Fixture(async r =>
        {
            Assert.Equal("/api/productos/7/imagen", r.RequestUri!.AbsolutePath);
            Assert.Equal("multipart/form-data", r.Content!.Headers.ContentType?.MediaType);
            var body = await r.Content.ReadAsStringAsync();
            Assert.Contains("name=File", body);
            Assert.Contains("filename=foto.png", body);
            Assert.Contains("Content-Type: image/png", body);
            return Json(new { id = 1, productoId = 7, nombreArchivo = "foto.png", imagenUrl = "/img/foto.png", tipoContenido = "image/png", tamanoBytes = 3L });
        });

        await using var content = new MemoryStream([1, 2, 3]);
        var image = await new CatalogoClient(f.Api).SubirImagenAsync(7, "foto.png", "image/png", content);
        Assert.Equal(7, image.ProductoId);
    }

    [Fact]
    public async Task ZeroQuantityRemovesLineWithoutReadingJson()
    {
        using var f = new Fixture(r =>
        {
            Assert.Equal(HttpMethod.Put, r.Method);
            Assert.Equal("/api/salidas/5/detalles/8", r.RequestUri!.AbsolutePath);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NoContent));
        });

        Assert.Null(await new CarritoClient(f.Api).EditarDetalleAsync(5, 8, 0));
    }

    [Fact]
    public async Task ClientConfirmationSendsPaymentMethod()
    {
        using var f = new Fixture(async r =>
        {
            Assert.Equal(HttpMethod.Post, r.Method);
            Assert.Equal("/api/salidas/5/confirmar-cliente", r.RequestUri!.AbsolutePath);
            using var body = JsonDocument.Parse(await r.Content!.ReadAsStringAsync());
            Assert.Equal("Tarjeta", body.RootElement.GetProperty("medioPago").GetString());
            return Json(Sale(5, client: true));
        });

        var sale = await new CarritoClient(f.Api).ConfirmarClienteAsync(5, "Tarjeta");
        Assert.True(sale.ConfirmarXCliente);
    }

    [Fact]
    public async Task ReceiptRequestUsesSaleRouteAndAcceptsPdf()
    {
        using var f = new Fixture(r =>
        {
            Assert.Equal(HttpMethod.Get, r.Method);
            Assert.Equal("/api/salidas/5/comprobante", r.RequestUri!.AbsolutePath);
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(Encoding.ASCII.GetBytes("%PDF-1.4\ncomprobante"))
            };
            response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
            response.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment")
            {
                FileNameStar = "comprobante-5.pdf"
            };
            return Task.FromResult(response);
        });

        var file = await new CarritoClient(f.Api).ComprobanteAsync(5);
        Assert.Equal("application/pdf", file.MimeType);
        Assert.Equal("comprobante-5.pdf", file.FileName);
        Assert.StartsWith("%PDF-", Encoding.ASCII.GetString(file.Bytes));
    }

    [Fact]
    public async Task ActivePreferredDraftAvoidsListRequest()
    {
        var requests = 0;
        using var f = new Fixture(r =>
        {
            requests++;
            Assert.Equal("/api/salidas/9", r.RequestUri!.AbsolutePath);
            return Task.FromResult(Json(Sale(9)));
        });

        var draft = await new CarritoClient(f.Api).AsegurarBorradorAsync(9);
        Assert.Equal(9, draft.Id);
        Assert.Equal(1, requests);
    }

    [Fact]
    public async Task MissingPreferredDraftReusesNewestExistingDraft()
    {
        using var f = new Fixture(r => r.RequestUri!.AbsolutePath switch
        {
            "/api/salidas/9" => Task.FromResult(Json(new { error = "no" }, HttpStatusCode.NotFound)),
            "/api/salidas" => Task.FromResult(Json(new
            {
                page = 1, pageSize = 100, totalCount = 2, totalPages = 1, hasNext = false, hasPrevious = false,
                items = new[] { Sale(6, client: true), Sale(5) }
            })),
            _ => throw new Exception("Unexpected request")
        });

        var draft = await new CarritoClient(f.Api).AsegurarBorradorAsync(9);
        Assert.Equal(5, draft.Id);
    }

    [Fact]
    public async Task DraftListIgnoresConfirmedAndDeletedSales()
    {
        using var f = new Fixture(_ => Task.FromResult(Json(new
        {
            page = 1, pageSize = 100, totalCount = 3, totalPages = 1, hasNext = false, hasPrevious = false,
            items = new[] { Sale(3, employee: true), Sale(2, deleted: true), Sale(1) }
        })));

        var drafts = await new CarritoClient(f.Api).ListarBorradoresAsync();
        Assert.Single(drafts);
        Assert.Equal(1, drafts[0].Id);
    }

    [Fact]
    public async Task ReceiptCreateSendsProviderAndTrimmedObservation()
    {
        using var f = new Fixture(async r =>
        {
            using var body = JsonDocument.Parse(await r.Content!.ReadAsStringAsync());
            Assert.Equal(3, body.RootElement.GetProperty("proveedorId").GetInt32());
            Assert.Equal("reposición", body.RootElement.GetProperty("observaciones").GetString());
            return Json(new
            {
                id = 11, proveedorId = 3, empleadoId = 2, fecha = "2026-10-29T10:00:00Z", observaciones = "reposición",
                confirmado = false, fechaConfirmacion = (DateTime?)null, empleadoConfirmacionId = (int?)null,
                detalles = Array.Empty<object>(), total = 0m
            });
        });

        await new IngresosClient(f.Api).CrearAsync(new() { ProveedorId = 3, Observaciones = " reposición " });
    }

    [Fact]
    public async Task ClientCatalogAndDetailUseExpectedPaths()
    {
        var requests = new List<string>();
        using var f = new Fixture(r =>
        {
            requests.Add(r.RequestUri!.AbsolutePath);
            return Task.FromResult(r.RequestUri.AbsolutePath switch
            {
                "/api/clientes/catalogo" => Json(new[]
                {
                    new { id = 4, nombre = "Ana", apellido = "Pérez", cuit = "20-1", email = "ana@example.test", telefono = (string?)null, direccion = (string?)null }
                }),
                "/api/clientes/4" => Json(new { id = 4, nombre = "Ana", apellido = "Pérez", cuit = "20-1", email = "ana@example.test", telefono = "555", direccion = "Calle 1" }),
                _ => throw new Exception("Unexpected request")
            });
        });

        var client = new ClientesClient(f.Api);
        var catalog = await client.CatalogoAsync();
        var detail = await client.ClienteAsync(4);

        Assert.Equal(["/api/clientes/catalogo", "/api/clientes/4"], requests);
        Assert.Equal("Pérez", catalog[0].Apellido);
        Assert.Equal("555", detail.Telefono);
    }

    [Fact]
    public async Task ReceiptLineUpdateOmitsProductId()
    {
        using var f = new Fixture(async r =>
        {
            Assert.Equal(HttpMethod.Put, r.Method);
            Assert.Equal("/api/ingresos/11/detalles/3", r.RequestUri!.AbsolutePath);
            using var body = JsonDocument.Parse(await r.Content!.ReadAsStringAsync());
            Assert.Equal(7, body.RootElement.GetProperty("cantidad").GetInt32());
            Assert.Equal(5m, body.RootElement.GetProperty("descAplicado").GetDecimal());
            Assert.False(body.RootElement.TryGetProperty("productoId", out _));
            return Json(new
            {
                id = 3, productoId = 9, cantidad = 7, precioUnitario = 100m, descAplicado = 5m, subtotal = 665m
            });
        });

        await new IngresosClient(f.Api).EditarDetalleAsync(11, 3,
            new() { ProductoId = 9, Cantidad = 7, DescAplicado = 5m });
    }

    [Theory]
    [InlineData("2026-10-29T03:00:00.0000000Z", "2026-10-30T02:59:59.9999999Z")]
    public void CommerceDateBoundariesUseArgentineUtcMinusThree(string start, string end)
    {
        Assert.Equal(start, ComercioDates.Start(new DateTime(2026, 10, 29)));
        Assert.Equal(end, ComercioDates.End(new DateTime(2026, 10, 29)));
    }
}
