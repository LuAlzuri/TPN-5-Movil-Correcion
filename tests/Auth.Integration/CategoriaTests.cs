using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TPN_5_MOVIL.Data;
using TPN_5_MOVIL.DTOs;
using TPN_5_MOVIL.Entities;
using Xunit;

namespace Auth.Integration;

public class CategoriaTests
{
    [Fact]
    public void Sql_server_model_matches_existing_migrations_without_connecting()
    {
        using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer("Server=unused;Database=ModelOnly;Integrated Security=true").Options);
        Assert.False(db.Database.HasPendingModelChanges());
    }

    private static async Task<HttpClient> Client(AuthApp app, string role = "Administrador")
    {
        var client = app.CreateClient();
        await app.InitializeAsync();
        await Login(client, "admin@example.test");
        if (role != "Administrador")
        {
            var response = await client.PostAsJsonAsync("/api/auth/register", AuthApp.Registration(role));
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            await Login(client, "person@example.test");
        }
        return client;
    }

    private static async Task Login(HttpClient client, string email)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest { Email = email, Password = AuthApp.Password });
        response.EnsureSuccessStatusCode();
        var login = (await response.Content.ReadFromJsonAsync<LoginResponse>())!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.Token);
    }

    [Theory]
    [InlineData("Administrador")]
    [InlineData("Empleado")]
    [InlineData("Cliente")]
    public async Task Catalog_is_active_sorted_and_minimal_for_all_roles(string role)
    {
        using var app = new AuthApp();
        using var client = await Client(app, role);
        await app.InDatabase(async db =>
        {
            db.Categorias.AddRange(new Categoria { Nombre = "Zeta" }, new Categoria { Nombre = "Alfa" },
                new Categoria { Nombre = "Oculta", Eliminado = true });
            await db.SaveChangesAsync();
        });
        var response = await client.GetAsync("/api/categorias/catalogo");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var items = (await response.Content.ReadFromJsonAsync<JsonElement>()).EnumerateArray().ToArray();
        Assert.Equal(new[] { "Alfa", "Zeta" }, items.Select(x => x.GetProperty("nombre").GetString()));
        Assert.All(items, x => Assert.Equal(new[] { "id", "nombre" }, x.EnumerateObject().Select(p => p.Name)));
    }

    [Theory]
    [InlineData("Empleado")]
    [InlineData("Cliente")]
    public async Task Other_roles_cannot_use_administrative_routes(string role)
    {
        using var app = new AuthApp();
        using var client = await Client(app, role);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/categorias")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/categorias/1")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync("/api/categorias", new { nombre = "A" })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PutAsJsonAsync("/api/categorias/1", new { nombre = "A" })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.DeleteAsync("/api/categorias/1")).StatusCode);
    }

    [Fact]
    public async Task Anonymous_cannot_use_any_category_route()
    {
        using var app = new AuthApp();
        using var client = app.CreateClient();
        foreach (var route in new[] { "/api/categorias", "/api/categorias/catalogo", "/api/categorias/1" })
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync(route)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/categorias", new { nombre = "A" })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PutAsJsonAsync("/api/categorias/1", new { nombre = "A" })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.DeleteAsync("/api/categorias/1")).StatusCode);
    }

    [Fact]
    public async Task Validation_paging_and_search_are_enforced()
    {
        using var app = new AuthApp();
        using var client = await Client(app);
        foreach (var name in new string?[] { null, "", "   ", new string('a', 101) })
        {
            Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/categorias", new { nombre = name })).StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, (await client.PutAsJsonAsync("/api/categorias/1", new { nombre = name })).StatusCode);
        }
        foreach (var method in new[] { HttpMethod.Post, HttpMethod.Put })
        {
            using var request = new HttpRequestMessage(method, method == HttpMethod.Post ? "/api/categorias" : "/api/categorias/1")
                { Content = JsonContent.Create(new { nombre = "A", descripcion = new string('x', 1001) }) };
            Assert.Equal(HttpStatusCode.BadRequest, (await client.SendAsync(request)).StatusCode);
        }
        foreach (var query in new[] { "page=0", "page=-1", "pageSize=0", "pageSize=101", "page=2147483647&pageSize=100" })
            Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync("/api/categorias?" + query)).StatusCode);
        foreach (var name in new[] { "Zeta", "Beta", "Alfa" })
            Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync("/api/categorias", new { nombre = name })).StatusCode);
        var page = (await client.GetFromJsonAsync<PaginatedResult<CategoriaResponse>>("/api/categorias?page=2&pageSize=1"))!;
        Assert.Equal(3, page.TotalCount);
        Assert.Equal("Beta", Assert.Single(page.Items).Nombre);
        Assert.True(page.HasNext && page.HasPrevious);
        var search = (await client.GetFromJsonAsync<PaginatedResult<CategoriaResponse>>("/api/categorias?busqueda=%20Alfa%20"))!;
        Assert.Equal("Alfa", Assert.Single(search.Items).Nombre);
        Assert.Empty((await client.GetFromJsonAsync<PaginatedResult<CategoriaResponse>>("/api/categorias?page=9"))!.Items);
    }

    [Fact]
    public async Task Crud_preserves_deleted_category_and_products_and_blocks_new_associations()
    {
        using var app = new AuthApp();
        using var client = await Client(app);
        var created = await client.PostAsJsonAsync("/api/categorias", new { nombre = "  Categoria  ", descripcion = "  Texto  " });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.NotNull(created.Headers.Location);
        var dto = (await created.Content.ReadFromJsonAsync<CategoriaResponse>())!;
        Assert.Equal("Categoria", dto.Nombre);
        Assert.Equal("Texto", dto.Descripcion);
        var route = "/api/categorias/" + dto.Id;
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(created.Headers.Location)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync("/api/categorias", new { nombre = " Categoria " })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.PutAsJsonAsync(route, new { nombre = "Categoria", descripcion = " " })).StatusCode);
        Assert.Null((await client.GetFromJsonAsync<CategoriaResponse>(route))!.Descripcion);
        var other = await client.PostAsJsonAsync("/api/categorias", new { nombre = "Otra" });
        var otherId = (await other.Content.ReadFromJsonAsync<CategoriaResponse>())!.Id;
        Assert.Equal(HttpStatusCode.Conflict, (await client.PutAsJsonAsync(route, new { nombre = "Otra" })).StatusCode);
        int productId = 0;
        await app.InDatabase(async db =>
        {
            var product = new Producto { Nombre = "Historico", CategoriaId = dto.Id };
            db.Productos.Add(product);
            await db.SaveChangesAsync();
            productId = product.Id;
        });
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync(route)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync(route)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PutAsJsonAsync(route, new { nombre = "Nueva" })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.DeleteAsync(route)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync("/api/categorias", new { nombre = "Categoria" })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PutAsJsonAsync("/api/categorias/" + otherId, new { nombre = "Categoria" })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsJsonAsync("/api/productos", new { nombre = "Nuevo", precio = 1, categoriaId = dto.Id })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PutAsJsonAsync("/api/productos/" + productId, new { nombre = "Historico", precio = 1, categoriaId = dto.Id })).StatusCode);
        await app.InDatabase(async db =>
        {
            Assert.True((await db.Categorias.IgnoreQueryFilters().SingleAsync(c => c.Id == dto.Id)).Eliminado);
            Assert.Equal(dto.Id, (await db.Productos.IgnoreQueryFilters().SingleAsync(p => p.Id == productId)).CategoriaId);
        });
        Assert.DoesNotContain((await client.GetFromJsonAsync<CategoriaCatalogoResponse[]>("/api/categorias/catalogo"))!, c => c.Id == dto.Id);
        Assert.DoesNotContain((await client.GetFromJsonAsync<PaginatedResult<CategoriaResponse>>("/api/categorias"))!.Items, c => c.Id == dto.Id);
    }
}
