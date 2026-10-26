using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using TPN_5_MOVIL.DTOs;
using TPN_5_MOVIL.Entities;
using Xunit;

namespace Auth.Integration;

public class ProveedorTests
{
    private static Dictionary<string, object?> Request(string cuit = "20-12345678-9") => new()
    {
        ["nombre"] = " Proveedor ", ["cuit"] = cuit, ["email"] = " MAIL@EXAMPLE.TEST ",
        ["telefono"] = " 1234 ", ["direccion"] = " Calle 123 "
    };

    private static async Task Login(HttpClient client, string email)
    {
        var login = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest { Email = email, Password = AuthApp.Password });
        login.EnsureSuccessStatusCode();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            (await login.Content.ReadFromJsonAsync<LoginResponse>())!.Token);
    }

    private static async Task<HttpClient> Client(AuthApp app, string role = "Administrador")
    {
        var client = app.CreateClient();
        await app.InitializeAsync();
        await Login(client, "admin@example.test");
        if (role != "Administrador")
        {
            Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync("/api/auth/register", AuthApp.Registration(role))).StatusCode);
            await Login(client, "person@example.test");
        }
        return client;
    }

    private static async Task<ProveedorResponse> Create(HttpClient client, string cuit = "20-12345678-9")
    {
        var response = await client.PostAsJsonAsync("/api/proveedores", Request(cuit));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        var supplier = (await response.Content.ReadFromJsonAsync<ProveedorResponse>())!;
        Assert.Equal(supplier, await client.GetFromJsonAsync<ProveedorResponse>(response.Headers.Location));
        return supplier;
    }

    [Theory]
    [InlineData("Administrador")]
    [InlineData("Empleado")]
    public async Task Both_operational_roles_can_use_all_supplier_routes(string role)
    {
        using var app = new AuthApp();
        using var client = await Client(app, role);
        var supplier = await Create(client);
        Assert.Equal("Proveedor", supplier.Nombre);
        Assert.Equal("mail@example.test", supplier.Email);
        Assert.Equal("1234", supplier.Telefono);
        Assert.Equal("Calle 123", supplier.Direccion);
        Assert.Equal(supplier.Id, Assert.Single((await client.GetFromJsonAsync<PaginatedResult<ProveedorResponse>>("/api/proveedores"))!.Items).Id);
        Assert.Equal(supplier.Id, Assert.Single((await client.GetFromJsonAsync<ProveedorResponse[]>("/api/proveedores/catalogo"))!).Id);
        var other = await Create(client, "27-12345678-0"); // Supplier emails are not unique.
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync("/api/proveedores", Request("20123456789"))).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PutAsJsonAsync("/api/proveedores/" + other.Id, Request("20 12345678 9"))).StatusCode);
        var data = Request("20 12345678 9");
        data["nombre"] = " Editado ";
        data["email"] = " ";
        data["telefono"] = "";
        data["direccion"] = null;
        var route = "/api/proveedores/" + supplier.Id;
        Assert.Equal(HttpStatusCode.NoContent, (await client.PutAsJsonAsync(route, data)).StatusCode);
        var updated = (await client.GetFromJsonAsync<ProveedorResponse>(route))!;
        Assert.Equal("Editado", updated.Nombre);
        Assert.Null(updated.Email);
        Assert.Null(updated.Telefono);
        Assert.Null(updated.Direccion);
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync(route)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync(route)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PutAsJsonAsync(route, data)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.DeleteAsync(route)).StatusCode);
        Assert.DoesNotContain((await client.GetFromJsonAsync<ProveedorResponse[]>("/api/proveedores/catalogo"))!, p => p.Id == supplier.Id);
        Assert.DoesNotContain((await client.GetFromJsonAsync<PaginatedResult<ProveedorResponse>>("/api/proveedores"))!.Items, p => p.Id == supplier.Id);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync("/api/proveedores", Request("20123456789"))).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PutAsJsonAsync("/api/proveedores/" + other.Id, Request("20123456789"))).StatusCode);
        foreach (var unknown in new[] { 0, int.MaxValue })
        {
            Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/proveedores/" + unknown)).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await client.PutAsJsonAsync("/api/proveedores/" + unknown, Request())).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await client.DeleteAsync("/api/proveedores/" + unknown)).StatusCode);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Anonymous_and_customer_cannot_access_any_supplier_route(bool customer)
    {
        using var app = new AuthApp();
        using var client = await Client(app, customer ? "Cliente" : "Administrador");
        if (!customer) client.DefaultRequestHeaders.Authorization = null;
        var expected = customer ? HttpStatusCode.Forbidden : HttpStatusCode.Unauthorized;
        foreach (var route in new[] { "/api/proveedores", "/api/proveedores/catalogo", "/api/proveedores/1" })
            Assert.Equal(expected, (await client.GetAsync(route)).StatusCode);
        Assert.Equal(expected, (await client.PostAsJsonAsync("/api/proveedores", Request())).StatusCode);
        Assert.Equal(expected, (await client.PutAsJsonAsync("/api/proveedores/1", Request())).StatusCode);
        Assert.Equal(expected, (await client.DeleteAsync("/api/proveedores/1")).StatusCode);
    }

    [Theory]
    [InlineData("nombre", null)]
    [InlineData("nombre", "")]
    [InlineData("nombre", " ")]
    [InlineData("nombre", "long")]
    [InlineData("cuit", null)]
    [InlineData("cuit", "-- --")]
    [InlineData("cuit", "long")]
    [InlineData("email", "invalid")]
    [InlineData("email", "long")]
    [InlineData("telefono", "long")]
    [InlineData("direccion", "long")]
    [InlineData("id", "1")]
    [InlineData("eliminado", "true")]
    public async Task Invalid_or_unexpected_data_is_rejected_without_changing_supplier(string field, string? value)
    {
        using var app = new AuthApp();
        using var client = await Client(app);
        var supplier = await Create(client);
        var data = Request("27-12345678-0");
        var maximum = field switch { "cuit" => 20, "telefono" => 50, "direccion" => 300, _ => 150 };
        data[field] = value == "long" ? (field == "email" ? new string('a', 140) + "@example.test" : new string('x', maximum + 1)) : value;
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/proveedores", data)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PutAsJsonAsync("/api/proveedores/" + supplier.Id, data)).StatusCode);
        Assert.Equal(supplier, await client.GetFromJsonAsync<ProveedorResponse>("/api/proveedores/" + supplier.Id));
        await app.InDatabase(async db => Assert.Equal(1, await db.Proveedores.CountAsync()));
    }

    [Fact]
    public async Task Optional_fields_can_be_omitted_empty_or_at_maximum_lengths()
    {
        using var app = new AuthApp();
        using var client = await Client(app);
        var created = await client.PostAsJsonAsync("/api/proveedores", new { nombre = "Minimo", cuit = "123" });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var supplier = (await created.Content.ReadFromJsonAsync<ProveedorResponse>())!;
        Assert.Null(supplier.Email);
        Assert.Null(supplier.Telefono);
        Assert.Null(supplier.Direccion);
        var data = Request(new string('1', 20));
        data["nombre"] = new string('N', 150);
        data["email"] = new string('a', 137) + "@example.test";
        data["telefono"] = new string('1', 50);
        data["direccion"] = new string('D', 300);
        var route = "/api/proveedores/" + supplier.Id;
        Assert.Equal(HttpStatusCode.NoContent, (await client.PutAsJsonAsync(route, data)).StatusCode);
        var updated = (await client.GetFromJsonAsync<ProveedorResponse>(route))!;
        Assert.Equal(150, updated.Nombre.Length);
        Assert.Equal(20, updated.CUIT.Length);
        Assert.Equal(150, updated.Email!.Length);
        Assert.Equal(50, updated.Telefono!.Length);
        Assert.Equal(300, updated.Direccion!.Length);
        foreach (var email in new string?[] { null, "", " \t " })
        {
            data["email"] = email;
            Assert.Equal(HttpStatusCode.NoContent, (await client.PutAsJsonAsync(route, data)).StatusCode);
            Assert.Null((await client.GetFromJsonAsync<ProveedorResponse>(route))!.Email);
        }
        Assert.Equal(HttpStatusCode.NoContent, (await client.PutAsJsonAsync(route, new { nombre = "Minimo", cuit = "123" })).StatusCode);
        var cleared = (await client.GetFromJsonAsync<ProveedorResponse>(route))!;
        Assert.Null(cleared.Email);
        Assert.Null(cleared.Telefono);
        Assert.Null(cleared.Direccion);
    }

    [Fact]
    public async Task Search_paging_and_catalog_are_stable_and_handle_null_email()
    {
        using var app = new AuthApp();
        using var client = await Client(app);
        Assert.Empty((await client.GetFromJsonAsync<PaginatedResult<ProveedorResponse>>("/api/proveedores"))!.Items);
        var ids = new List<int>();
        foreach (var name in new[] { "Zeta", "Alfa", "Alfa" })
        {
            var data = Request(((ids.Count + 1) * 111).ToString());
            data["nombre"] = name;
            data["email"] = ids.Count == 1 ? "find@example.test" : null;
            var result = await client.PostAsJsonAsync("/api/proveedores", data);
            Assert.Equal(HttpStatusCode.Created, result.StatusCode);
            ids.Add((await result.Content.ReadFromJsonAsync<ProveedorResponse>())!.Id);
        }
        var first = (await client.GetFromJsonAsync<PaginatedResult<ProveedorResponse>>("/api/proveedores?pageSize=2"))!;
        var second = (await client.GetFromJsonAsync<PaginatedResult<ProveedorResponse>>("/api/proveedores?page=2&pageSize=2"))!;
        Assert.Equal(new[] { ids[1], ids[2] }, first.Items.Select(p => p.Id));
        Assert.Equal(ids[0], Assert.Single(second.Items).Id);
        Assert.Equal(3, second.TotalCount);
        Assert.Equal(2, second.TotalPages);
        Assert.True(first.HasNext && !first.HasPrevious && second.HasPrevious && !second.HasNext);
        Assert.Equal(new[] { ids[1], ids[2], ids[0] }, (await client.GetFromJsonAsync<ProveedorResponse[]>("/api/proveedores/catalogo"))!.Select(p => p.Id));
        Assert.Equal(2, (await client.GetFromJsonAsync<PaginatedResult<ProveedorResponse>>("/api/proveedores?busqueda=%20Alfa%20"))!.TotalCount);
        Assert.Equal(ids[1], Assert.Single((await client.GetFromJsonAsync<PaginatedResult<ProveedorResponse>>("/api/proveedores?busqueda=find@example.test"))!.Items).Id);
        Assert.Equal(ids[2], Assert.Single((await client.GetFromJsonAsync<PaginatedResult<ProveedorResponse>>("/api/proveedores?busqueda=333"))!.Items).Id);
        Assert.Empty((await client.GetFromJsonAsync<PaginatedResult<ProveedorResponse>>("/api/proveedores?page=99"))!.Items);
        foreach (var query in new[] { "page=0", "page=-1", "pageSize=0", "pageSize=101", "page=2147483647&pageSize=100", "page=abc" })
            Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync("/api/proveedores?" + query)).StatusCode);
    }

    [Fact]
    public async Task Soft_delete_preserves_confirmed_income_details_and_stock()
    {
        using var app = new AuthApp();
        using var client = await Client(app);
        var supplier = await Create(client);
        var registration = await client.PostAsJsonAsync("/api/auth/register", AuthApp.Registration("Empleado"));
        var workerId = (await registration.Content.ReadFromJsonAsync<UsuarioDto>())!.Id;
        int incomeId = 0;
        int productId = 0;
        await app.InDatabase(async db =>
        {
            var product = new Producto { Nombre = "Historico", Categoria = new Categoria { Nombre = "Categoria" }, Stock = 5 };
            var income = new Ingreso { ProveedorId = supplier.Id, EmpleadoId = workerId, Confirmado = true,
                EmpleadoConfirmacionId = workerId, FechaConfirmacion = DateTime.UtcNow };
            income.Detalles.Add(new DetalleIngreso { Producto = product, Cantidad = 5, PrecioUnitario = 10 });
            db.Ingresos.Add(income);
            await db.SaveChangesAsync();
            incomeId = income.Id;
            productId = product.Id;
        });
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync("/api/proveedores/" + supplier.Id)).StatusCode);
        Assert.Empty((await client.GetFromJsonAsync<ProveedorResponse[]>("/api/proveedores/catalogo"))!);
        await app.InDatabase(async db =>
        {
            Assert.False(await db.Proveedores.AnyAsync(p => p.Id == supplier.Id));
            Assert.True((await db.Proveedores.IgnoreQueryFilters().SingleAsync()).Eliminado);
            Assert.Equal(supplier.Id, (await db.Ingresos.IgnoreQueryFilters().SingleAsync(i => i.Id == incomeId)).ProveedorId);
            Assert.Equal(productId, (await db.DetallesIngreso.IgnoreQueryFilters().SingleAsync()).ProductoId);
            Assert.Equal(5, (await db.Productos.SingleAsync(p => p.Id == productId)).Stock);
        });
    }
}
