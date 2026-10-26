using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using TPN_5_MOVIL.DTOs;
using TPN_5_MOVIL.Entities;
using Xunit;

namespace Auth.Integration;

public class EmpleadoTests
{
    private static object Request(string email = "employee@example.test") => new
    {
        nombre = " Nombre ", apellido = " Apellido ", cuit = " 123 ", email,
        password = AuthApp.Password, fechaIngreso = "2026-01-01"
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

    [Theory]
    [InlineData(null)]
    [InlineData("Empleado")]
    [InlineData("Cliente")]
    public async Task All_employee_routes_require_administrator(string? role)
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
        Assert.Equal(expected, (await client.GetAsync("/api/empleados")).StatusCode);
        Assert.Equal(expected, (await client.GetAsync("/api/empleados/1")).StatusCode);
        Assert.Equal(expected, (await client.PostAsJsonAsync("/api/empleados", Request())).StatusCode);
        Assert.Equal(expected, (await client.PutAsJsonAsync("/api/empleados/1", new { })).StatusCode);
        Assert.Equal(expected, (await client.DeleteAsync("/api/empleados/1")).StatusCode);
    }

    [Fact]
    public async Task Create_update_delete_preserve_profile_history_and_revoke_access()
    {
        using var app = new AuthApp();
        using var admin = await Admin(app);
        var created = await admin.PostAsJsonAsync("/api/empleados", Request("EMPLOYEE@example.test"));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.NotNull(created.Headers.Location);
        var employee = (await created.Content.ReadFromJsonAsync<EmpleadoResponse>())!;
        Assert.Equal("Nombre", employee.Nombre);
        Assert.Equal("Apellido", employee.Apellido);
        Assert.Equal("123", employee.CUIT);
        Assert.Equal("employee@example.test", employee.Email);
        Assert.DoesNotContain("password", (await created.Content.ReadAsStringAsync()).ToLowerInvariant());
        Assert.Equal(employee, await admin.GetFromJsonAsync<EmpleadoResponse>(created.Headers.Location));
        using var worker = app.CreateClient();
        await Login(worker, employee.Email);
        await app.InDatabase(async db =>
        {
            var user = await db.Usuarios.Include(u => u.Empleado).Include(u => u.Cliente).SingleAsync(u => u.Id == employee.Id);
            Assert.Equal(Usuario.RolEmpleado, user.Rol);
            Assert.Equal(user.Id, user.Empleado!.Id);
            Assert.Null(user.Cliente);
            Assert.NotEqual(AuthApp.Password, user.PasswordHash);
            var supplier = new Proveedor { Nombre = "Proveedor", CUIT = "1" };
            db.Ingresos.Add(new Ingreso { EmpleadoId = employee.Id, Proveedor = supplier });
            await db.SaveChangesAsync();
        });
        var route = "/api/empleados/" + employee.Id;
        var update = new { nombre = " Nuevo ", apellido = " Apellido ", cuit = " 456 ", email = "NUEVO@example.test", fechaIngreso = "2026-02-02" };
        Assert.Equal(HttpStatusCode.NoContent, (await admin.PutAsJsonAsync(route, update)).StatusCode);
        var updated = (await admin.GetFromJsonAsync<EmpleadoResponse>(route))!;
        Assert.Equal("Nuevo", updated.Nombre);
        Assert.Equal("nuevo@example.test", updated.Email);
        Assert.Equal(new DateOnly(2026, 2, 2), updated.FechaIngreso);
        await Login(worker, updated.Email); // Password unchanged.
        Assert.Equal(HttpStatusCode.NoContent, (await admin.DeleteAsync(route)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await worker.GetAsync("/api/auth/me")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await worker.PostAsJsonAsync("/api/auth/login", new LoginRequest { Email = updated.Email, Password = AuthApp.Password })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync(route)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.PutAsJsonAsync(route, update)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.DeleteAsync(route)).StatusCode);
        Assert.Empty((await admin.GetFromJsonAsync<PaginatedResult<EmpleadoResponse>>("/api/empleados"))!.Items);
        await app.InDatabase(async db =>
        {
            Assert.True((await db.Usuarios.IgnoreQueryFilters().SingleAsync(u => u.Id == employee.Id)).Eliminado);
            Assert.Equal(employee.Id, (await db.Empleados.IgnoreQueryFilters().SingleAsync()).Id);
            Assert.Equal(employee.Id, (await db.Ingresos.IgnoreQueryFilters().SingleAsync()).EmpleadoId);
        });
        Assert.Equal(HttpStatusCode.Conflict, (await admin.PostAsJsonAsync("/api/empleados", Request(updated.Email))).StatusCode);
    }

    [Theory]
    [InlineData("nombre", " ")]
    [InlineData("apellido", "")]
    [InlineData("cuit", null)]
    [InlineData("email", "invalid")]
    [InlineData("fechaIngreso", null)]
    [InlineData("fechaIngreso", "0001-01-01")]
    [InlineData("fechaIngreso", "2026-02-30")]
    [InlineData("nombre", "long")]
    [InlineData("apellido", "long")]
    [InlineData("cuit", "long")]
    [InlineData("email", "long")]
    [InlineData("rol", "Administrador")]
    public async Task Invalid_fields_are_rejected_on_create_and_update(string field, string? value)
    {
        using var app = new AuthApp();
        using var client = await Admin(app);
        var data = new Dictionary<string, object?>
        {
            ["nombre"] = "Nombre", ["apellido"] = "Apellido", ["cuit"] = "123",
            ["email"] = "employee@example.test", ["fechaIngreso"] = "2026-01-01"
        };
        data[field] = value == "long" ? new string('a', field == "cuit" ? 21 : 151) : value;
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PutAsJsonAsync("/api/empleados/1", data)).StatusCode);
        data["password"] = AuthApp.Password;
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/empleados", data)).StatusCode);
        await app.InDatabase(async db => Assert.Equal(1, await db.Usuarios.CountAsync()));
    }

    [Fact]
    public async Task Password_policy_duplicates_and_non_employee_ids_are_enforced()
    {
        using var app = new AuthApp();
        using var client = await Admin(app);
        foreach (var password in new string?[] { null, "short", new string('x', 129), new string(' ', 12) })
            Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/empleados", new
            { nombre = "N", apellido = "A", cuit = "1", email = "e@example.test", password, fechaIngreso = "2026-01-01" })).StatusCode);
        var created = await client.PostAsJsonAsync("/api/empleados", Request());
        var employee = (await created.Content.ReadFromJsonAsync<EmpleadoResponse>())!;
        var route = "/api/empleados/" + employee.Id;
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync("/api/empleados", Request("EMPLOYEE@example.test"))).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PutAsJsonAsync(route, new
        { nombre = "N", apellido = "A", cuit = "1", email = "ADMIN@example.test", fechaIngreso = "2026-01-01" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PutAsJsonAsync(route, new
        { nombre = "N", apellido = "A", cuit = "1", email = employee.Email, fechaIngreso = "2026-01-01", password = AuthApp.Password })).StatusCode);
        var admin = (await client.GetFromJsonAsync<UsuarioDto>("/api/auth/me"))!;
        var customer = await client.PostAsJsonAsync("/api/auth/register", AuthApp.Registration("Cliente"));
        var customerId = (await customer.Content.ReadFromJsonAsync<UsuarioDto>())!.Id;
        foreach (var id in new[] { admin.Id, customerId, int.MaxValue })
        {
            Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/empleados/" + id)).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await client.PutAsJsonAsync("/api/empleados/" + id, new
            { nombre = "N", apellido = "A", cuit = "1", email = "ok@example.test", fechaIngreso = "2026-01-01" })).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await client.DeleteAsync("/api/empleados/" + id)).StatusCode);
        }
    }

    [Fact]
    public async Task Registration_rolls_back_if_employee_profile_fails()
    {
        using var app = new AuthApp();
        using var client = await Admin(app);
        await app.InDatabase(db => db.Database.ExecuteSqlRawAsync(db.Database.IsSqlServer()
            ? "ALTER TABLE Empleados ADD CONSTRAINT CK_RejectTestEmployee CHECK (Id < 0);"
            : "CREATE TRIGGER reject_employee BEFORE INSERT ON Empleados BEGIN SELECT RAISE(ABORT, 'injected profile failure'); END;"));
        Assert.Equal(HttpStatusCode.InternalServerError, (await client.PostAsJsonAsync("/api/empleados", Request())).StatusCode);
        await app.InDatabase(async db =>
        {
            Assert.Equal(1, await db.Usuarios.CountAsync());
            Assert.Empty(await db.Empleados.ToListAsync());
        });
    }

    [Fact]
    public async Task Profile_update_failure_rolls_back_account_changes()
    {
        using var app = new AuthApp();
        using var client = await Admin(app);
        var created = await client.PostAsJsonAsync("/api/empleados", Request());
        var employee = (await created.Content.ReadFromJsonAsync<EmpleadoResponse>())!;
        await app.InDatabase(db => db.Database.ExecuteSqlRawAsync(db.Database.IsSqlServer()
            ? "CREATE TRIGGER reject_employee_update ON Empleados AFTER UPDATE AS THROW 50001, 'injected update failure', 1;"
            : "CREATE TRIGGER reject_employee_update BEFORE UPDATE ON Empleados BEGIN SELECT RAISE(ABORT, 'injected update failure'); END;"));
        Assert.Equal(HttpStatusCode.InternalServerError, (await client.PutAsJsonAsync("/api/empleados/" + employee.Id, new
        { nombre = "Cambiado", apellido = "Otro", cuit = "456", email = "changed@example.test", fechaIngreso = "2026-03-03" })).StatusCode);
        Assert.Equal(employee, await client.GetFromJsonAsync<EmpleadoResponse>("/api/empleados/" + employee.Id));
    }

    [Fact]
    public async Task Paging_search_and_deleted_email_conflicts_are_consistent()
    {
        using var app = new AuthApp();
        using var client = await Admin(app);
        foreach (var query in new[] { "page=0", "page=-1", "pageSize=0", "pageSize=101", "page=2147483647&pageSize=100" })
            Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync("/api/empleados?" + query)).StatusCode);
        foreach (var email in new[] { "a@example.test", "b@example.test", "c@example.test" })
            Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync("/api/empleados", Request(email))).StatusCode);
        var first = (await client.GetFromJsonAsync<PaginatedResult<EmpleadoResponse>>("/api/empleados?pageSize=1"))!;
        var second = (await client.GetFromJsonAsync<PaginatedResult<EmpleadoResponse>>("/api/empleados?page=2&pageSize=1"))!;
        Assert.Equal(3, second.TotalCount);
        Assert.True(second.HasPrevious && second.HasNext);
        Assert.True(Assert.Single(first.Items).Id < Assert.Single(second.Items).Id);
        foreach (var term in new[] { "Nombre%20Apellido", "123", "a@example.test" })
            Assert.NotEmpty((await client.GetFromJsonAsync<PaginatedResult<EmpleadoResponse>>("/api/empleados?busqueda=%20" + term + "%20"))!.Items);
        Assert.Empty((await client.GetFromJsonAsync<PaginatedResult<EmpleadoResponse>>("/api/empleados?page=99"))!.Items);
        var deleted = first.Items.Single();
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync("/api/empleados/" + deleted.Id)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PutAsJsonAsync("/api/empleados/" + second.Items.Single().Id, new
        { nombre = "N", apellido = "A", cuit = "1", email = deleted.Email.ToUpperInvariant(), fechaIngreso = "2026-01-01" })).StatusCode);
        Assert.Equal(second.Items.Single(), await client.GetFromJsonAsync<EmpleadoResponse>("/api/empleados/" + second.Items.Single().Id));
    }
}
