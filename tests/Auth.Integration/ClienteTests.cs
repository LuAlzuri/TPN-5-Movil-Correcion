using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using TPN_5_MOVIL.DTOs;
using TPN_5_MOVIL.Entities;
using Xunit;

namespace Auth.Integration;

public class ClienteTests
{
    private static Dictionary<string, object?> Request(string email = "customer@example.test", string cuit = "20-12345678-9") => new()
    {
        ["nombre"] = " Nombre ", ["apellido"] = " Apellido ", ["cuit"] = cuit,
        ["email"] = email, ["telefono"] = " 1234 ", ["direccion"] = " Calle 123 "
    };

    private static async Task Login(HttpClient client, string email)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest { Email = email, Password = AuthApp.Password });
        response.EnsureSuccessStatusCode();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            (await response.Content.ReadFromJsonAsync<LoginResponse>())!.Token);
    }

    private static async Task<HttpClient> Admin(AuthApp app)
    {
        var client = app.CreateClient();
        await app.InitializeAsync();
        await Login(client, "admin@example.test");
        return client;
    }

    private static async Task<ClienteResponse> Create(HttpClient client,
        string email = "customer@example.test", string cuit = "20-12345678-9")
    {
        var data = Request(email, cuit);
        data["password"] = AuthApp.Password;
        var response = await client.PostAsJsonAsync("/api/clientes", data);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        var customer = (await response.Content.ReadFromJsonAsync<ClienteResponse>())!;
        Assert.Equal(customer, await client.GetFromJsonAsync<ClienteResponse>(response.Headers.Location));
        Assert.DoesNotContain("password", (await response.Content.ReadAsStringAsync()).ToLowerInvariant());
        return customer;
    }

    [Theory]
    [InlineData(null)]
    [InlineData("Empleado")]
    [InlineData("Cliente")]
    public async Task All_customer_routes_require_administrator(string? role)
    {
        using var app = new AuthApp();
        using var client = await Admin(app);
        if (role is null) client.DefaultRequestHeaders.Authorization = null;
        else
        {
            Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync("/api/auth/register", AuthApp.Registration(role))).StatusCode);
            await Login(client, "person@example.test");
        }
        var expected = role is null ? HttpStatusCode.Unauthorized : HttpStatusCode.Forbidden;
        foreach (var route in new[] { "/api/clientes", "/api/clientes/catalogo", "/api/clientes/1" })
            Assert.Equal(expected, (await client.GetAsync(route)).StatusCode);
        Assert.Equal(expected, (await client.PostAsJsonAsync("/api/clientes", new { })).StatusCode);
        Assert.Equal(expected, (await client.PutAsJsonAsync("/api/clientes/1", new { })).StatusCode);
        Assert.Equal(expected, (await client.DeleteAsync("/api/clientes/1")).StatusCode);
    }

    [Fact]
    public async Task Crud_keeps_shared_identity_password_and_history_and_revokes_deleted_account()
    {
        using var app = new AuthApp();
        using var client = await Admin(app);
        var customer = await Create(client, "CUSTOMER@example.test");
        Assert.Equal("Nombre", customer.Nombre);
        Assert.Equal("Apellido", customer.Apellido);
        Assert.Equal("customer@example.test", customer.Email);
        Assert.Equal("1234", customer.Telefono);
        Assert.Equal("Calle 123", customer.Direccion);
        string hash = "";
        await app.InDatabase(async db =>
        {
            var user = await db.Usuarios.Include(u => u.Cliente).Include(u => u.Empleado).SingleAsync(u => u.Id == customer.Id);
            Assert.Equal(Usuario.RolCliente, user.Rol);
            Assert.Equal(user.Id, user.Cliente!.Id);
            Assert.Null(user.Empleado);
            hash = user.PasswordHash;
            Assert.NotEqual(AuthApp.Password, hash);
            db.Salidas.Add(new Salida { ClienteId = customer.Id, Observaciones = "Historico conservado" });
            await db.SaveChangesAsync();
        });
        var route = "/api/clientes/" + customer.Id;
        var update = Request("NUEVO@example.test", "27-12345678-0");
        update["nombre"] = " Nuevo ";
        update["telefono"] = " ";
        update["direccion"] = null;
        Assert.Equal(HttpStatusCode.NoContent, (await client.PutAsJsonAsync(route, update)).StatusCode);
        var updated = (await client.GetFromJsonAsync<ClienteResponse>(route))!;
        Assert.Equal("Nuevo", updated.Nombre);
        Assert.Equal("nuevo@example.test", updated.Email);
        Assert.Null(updated.Telefono);
        Assert.Null(updated.Direccion);
        using var customerClient = app.CreateClient();
        await Login(customerClient, updated.Email);
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync(route)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await customerClient.GetAsync("/api/auth/me")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await customerClient.PostAsJsonAsync("/api/auth/login",
            new LoginRequest { Email = updated.Email, Password = AuthApp.Password })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync(route)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PutAsJsonAsync(route, update)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.DeleteAsync(route)).StatusCode);
        Assert.Empty((await client.GetFromJsonAsync<PaginatedResult<ClienteResponse>>("/api/clientes"))!.Items);
        Assert.Empty((await client.GetFromJsonAsync<ClienteResponse[]>("/api/clientes/catalogo"))!);
        await app.InDatabase(async db =>
        {
            var user = await db.Usuarios.IgnoreQueryFilters().SingleAsync(u => u.Id == customer.Id);
            Assert.True(user.Eliminado);
            Assert.Equal(hash, user.PasswordHash);
            Assert.Equal(Usuario.RolCliente, user.Rol);
            Assert.Equal(customer.Id, (await db.Clientes.IgnoreQueryFilters().SingleAsync()).Id);
            Assert.Equal(customer.Id, (await db.Salidas.IgnoreQueryFilters().SingleAsync()).ClienteId);
        });
    }

    [Theory]
    [InlineData("nombre", null)]
    [InlineData("nombre", " ")]
    [InlineData("apellido", "")]
    [InlineData("cuit", null)]
    [InlineData("cuit", "-- --")]
    [InlineData("email", "incorrecto")]
    [InlineData("nombre", "long")]
    [InlineData("apellido", "long")]
    [InlineData("cuit", "long")]
    [InlineData("email", "long")]
    [InlineData("telefono", "long")]
    [InlineData("direccion", "long")]
    [InlineData("rol", "Administrador")]
    [InlineData("id", "1")]
    [InlineData("fechaIngreso", "2026-01-01")]
    public async Task Invalid_or_unexpected_fields_are_rejected_on_create_and_update(string field, string? value)
    {
        using var app = new AuthApp();
        using var client = await Admin(app);
        var customer = await Create(client);
        var data = Request();
        var maximum = field switch { "cuit" => 20, "telefono" => 50, "direccion" => 300, _ => 150 };
        data[field] = value == "long" ? new string('x', maximum + 1) : value;
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PutAsJsonAsync("/api/clientes/" + customer.Id, data)).StatusCode);
        data["password"] = AuthApp.Password;
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/clientes", data)).StatusCode);
        Assert.Equal(customer, await client.GetFromJsonAsync<ClienteResponse>("/api/clientes/" + customer.Id));
        await app.InDatabase(async db => Assert.Equal(2, await db.Usuarios.CountAsync()));
    }

    [Fact]
    public async Task Password_is_required_only_at_creation_and_optional_fields_can_be_omitted()
    {
        using var app = new AuthApp();
        using var client = await Admin(app);
        foreach (var password in new string?[] { null, "short", new string('x', 129), new string(' ', 12) })
        {
            var invalid = Request();
            invalid["password"] = password;
            Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/clientes", invalid)).StatusCode);
        }
        var data = Request();
        data.Remove("telefono");
        data.Remove("direccion");
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/clientes", data)).StatusCode);
        data["password"] = AuthApp.Password;
        var created = await client.PostAsJsonAsync("/api/clientes", data);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var customer = (await created.Content.ReadFromJsonAsync<ClienteResponse>())!;
        Assert.Null(customer.Telefono);
        Assert.Null(customer.Direccion);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PutAsJsonAsync("/api/clientes/" + customer.Id, data)).StatusCode);
        data.Remove("password");
        data["telefono"] = new string('1', 50);
        data["direccion"] = new string('x', 300);
        Assert.Equal(HttpStatusCode.NoContent, (await client.PutAsJsonAsync("/api/clientes/" + customer.Id, data)).StatusCode);
        var updated = (await client.GetFromJsonAsync<ClienteResponse>("/api/clientes/" + customer.Id))!;
        Assert.Equal(50, updated.Telefono!.Length);
        Assert.Equal(300, updated.Direccion!.Length);
    }

    [Fact]
    public async Task Duplicate_emails_and_cuits_conflict_including_deleted_accounts()
    {
        using var app = new AuthApp();
        using var client = await Admin(app);
        var one = await Create(client, "one@example.test", "20-12345678-9");
        var two = await Create(client, "two@example.test", "27-12345678-0");
        foreach (var data in new[] { Request("ADMIN@example.test", "999"), Request("ONE@example.test", "999"), Request("three@example.test", "20123456789") })
        {
            data["password"] = AuthApp.Password;
            Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync("/api/clientes", data)).StatusCode);
        }
        Assert.Equal(HttpStatusCode.NoContent, (await client.PutAsJsonAsync("/api/clientes/" + one.Id, Request("ONE@example.test", "20 12345678 9"))).StatusCode);
        foreach (var data in new[] { Request("ADMIN@example.test", two.CUIT), Request("ONE@example.test", two.CUIT), Request(two.Email, "20123456789") })
            Assert.Equal(HttpStatusCode.Conflict, (await client.PutAsJsonAsync("/api/clientes/" + two.Id, data)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync("/api/clientes/" + one.Id)).StatusCode);
        foreach (var data in new[] { Request("ONE@example.test", "999"), Request("three@example.test", "20123456789") })
        {
            Assert.Equal(HttpStatusCode.Conflict, (await client.PutAsJsonAsync("/api/clientes/" + two.Id, data)).StatusCode);
            data["password"] = AuthApp.Password;
            Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync("/api/clientes", data)).StatusCode);
        }
        Assert.Equal(two, await client.GetFromJsonAsync<ClienteResponse>("/api/clientes/" + two.Id));
        await app.InDatabase(async db => Assert.Equal(3, await db.Usuarios.IgnoreQueryFilters().CountAsync()));
    }

    [Fact]
    public async Task Lists_and_catalog_use_stable_order_search_and_active_profiles()
    {
        using var app = new AuthApp();
        using var client = await Admin(app);
        Assert.Empty((await client.GetFromJsonAsync<ClienteResponse[]>("/api/clientes/catalogo"))!);
        var ids = new List<int>();
        foreach (var row in new[] { ("A", "Zeta"), ("B", "Alfa"), ("A", "Alfa"), ("A", "Alfa") })
        {
            var data = Request($"person{ids.Count}@example.test", ((ids.Count + 1) * 111).ToString());
            data["nombre"] = row.Item1;
            data["apellido"] = row.Item2;
            data["password"] = AuthApp.Password;
            var response = await client.PostAsJsonAsync("/api/clientes", data);
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            ids.Add((await response.Content.ReadFromJsonAsync<ClienteResponse>())!.Id);
        }
        var first = (await client.GetFromJsonAsync<PaginatedResult<ClienteResponse>>("/api/clientes?pageSize=2"))!;
        var second = (await client.GetFromJsonAsync<PaginatedResult<ClienteResponse>>("/api/clientes?page=2&pageSize=2"))!;
        Assert.Equal(new[] { ids[2], ids[3] }, first.Items.Select(c => c.Id));
        Assert.Equal(new[] { ids[1], ids[0] }, second.Items.Select(c => c.Id));
        Assert.Equal(4, second.TotalCount);
        Assert.Equal(2, second.TotalPages);
        Assert.True(first.HasNext && !first.HasPrevious && second.HasPrevious && !second.HasNext);
        Assert.Equal(new[] { ids[2], ids[3], ids[1], ids[0] }, (await client.GetFromJsonAsync<ClienteResponse[]>("/api/clientes/catalogo"))!.Select(c => c.Id));
        Assert.Equal(2, (await client.GetFromJsonAsync<PaginatedResult<ClienteResponse>>("/api/clientes?busqueda=%20A%20Alfa%20"))!.TotalCount);
        Assert.Equal(ids[2], Assert.Single((await client.GetFromJsonAsync<PaginatedResult<ClienteResponse>>("/api/clientes?busqueda=person2@example.test"))!.Items).Id);
        Assert.Equal(ids[2], Assert.Single((await client.GetFromJsonAsync<PaginatedResult<ClienteResponse>>("/api/clientes?busqueda=333"))!.Items).Id);
        Assert.Empty((await client.GetFromJsonAsync<PaginatedResult<ClienteResponse>>("/api/clientes?page=99"))!.Items);
        foreach (var query in new[] { "page=0", "page=-1", "pageSize=0", "pageSize=101", "page=2147483647&pageSize=100", "page=abc" })
            Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync("/api/clientes?" + query)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync("/api/clientes/" + ids[2])).StatusCode);
        Assert.DoesNotContain((await client.GetFromJsonAsync<ClienteResponse[]>("/api/clientes/catalogo"))!, c => c.Id == ids[2]);
        Assert.Equal(3, (await client.GetFromJsonAsync<PaginatedResult<ClienteResponse>>("/api/clientes"))!.TotalCount);
    }

    [Fact]
    public async Task Other_account_types_and_unknown_ids_cannot_be_modified_as_customers()
    {
        using var app = new AuthApp();
        using var client = await Admin(app);
        var admin = (await client.GetFromJsonAsync<UsuarioDto>("/api/auth/me"))!;
        var created = await client.PostAsJsonAsync("/api/auth/register", AuthApp.Registration("Empleado"));
        var worker = (await created.Content.ReadFromJsonAsync<UsuarioDto>())!;
        foreach (var id in new[] { admin.Id, worker.Id, int.MaxValue })
        {
            Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/clientes/" + id)).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await client.PutAsJsonAsync("/api/clientes/" + id, Request())).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await client.DeleteAsync("/api/clientes/" + id)).StatusCode);
        }
        await app.InDatabase(async db => Assert.Equal(2, await db.Usuarios.CountAsync()));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Inconsistent_profiles_are_not_exposed_or_modified(bool bothProfiles)
    {
        using var app = new AuthApp();
        using var client = await Admin(app);
        var customer = await Create(client);
        await app.InDatabase(async db =>
        {
            if (bothProfiles) db.Empleados.Add(new Empleado { Id = customer.Id, FechaIngreso = new DateOnly(2026, 1, 1) });
            else (await db.Usuarios.SingleAsync(u => u.Id == customer.Id)).Rol = Usuario.RolEmpleado;
            await db.SaveChangesAsync();
        });
        Assert.Empty((await client.GetFromJsonAsync<PaginatedResult<ClienteResponse>>("/api/clientes"))!.Items);
        Assert.Empty((await client.GetFromJsonAsync<ClienteResponse[]>("/api/clientes/catalogo"))!);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/clientes/" + customer.Id)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PutAsJsonAsync("/api/clientes/" + customer.Id, Request())).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.DeleteAsync("/api/clientes/" + customer.Id)).StatusCode);
    }

    [Fact]
    public async Task Failed_profile_creation_leaves_no_account()
    {
        using var app = new AuthApp();
        using var client = await Admin(app);
        await app.InDatabase(db => db.Database.ExecuteSqlRawAsync(db.Database.IsSqlServer()
            ? "ALTER TABLE Clientes ADD CONSTRAINT CK_RejectCustomerCreation CHECK (Id < 0);"
            : "CREATE TRIGGER reject_customer_creation BEFORE INSERT ON Clientes BEGIN SELECT RAISE(ABORT, 'injected profile failure'); END;"));
        var data = Request();
        data["password"] = AuthApp.Password;
        Assert.Equal(HttpStatusCode.InternalServerError, (await client.PostAsJsonAsync("/api/clientes", data)).StatusCode);
        await app.InDatabase(async db =>
        {
            Assert.Equal(1, await db.Usuarios.CountAsync());
            Assert.Empty(await db.Clientes.ToListAsync());
        });
    }

    [Fact]
    public async Task Failed_profile_update_rolls_back_personal_data_and_credentials_still_work()
    {
        using var app = new AuthApp();
        using var client = await Admin(app);
        var customer = await Create(client);
        await app.InDatabase(db => db.Database.ExecuteSqlRawAsync(db.Database.IsSqlServer()
            ? "CREATE TRIGGER reject_customer_update ON Clientes AFTER UPDATE AS THROW 50001, 'injected update failure', 1;"
            : "CREATE TRIGGER reject_customer_update BEFORE UPDATE ON Clientes BEGIN SELECT RAISE(ABORT, 'injected update failure'); END;"));
        var data = Request("changed@example.test", "27-12345678-0");
        data["nombre"] = "Cambiado";
        data["telefono"] = "9999";
        Assert.Equal(HttpStatusCode.InternalServerError, (await client.PutAsJsonAsync("/api/clientes/" + customer.Id, data)).StatusCode);
        Assert.Equal(customer, await client.GetFromJsonAsync<ClienteResponse>("/api/clientes/" + customer.Id));
        using var customerClient = app.CreateClient();
        await Login(customerClient, customer.Email);
    }
}
