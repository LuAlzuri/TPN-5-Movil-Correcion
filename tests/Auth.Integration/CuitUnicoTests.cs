using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TPN_5_MOVIL.DTOs;
using TPN_5_MOVIL.Entities;
using Xunit;

namespace Auth.Integration;

public class CuitUnicoTests
{
    private const string Cuit = "20-12345678-9";

    private static async Task<HttpClient> Admin(AuthApp app)
    {
        var client = app.CreateClient();
        await app.InitializeAsync();
        var login = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest
            { Email = "admin@example.test", Password = AuthApp.Password });
        login.EnsureSuccessStatusCode();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            (await login.Content.ReadFromJsonAsync<LoginResponse>())!.Token);
        return client;
    }

    private static object Registration(string email, string cuit, string role = "Cliente") => new
    {
        nombre = "Nombre", apellido = "Apellido", cuit, email, password = AuthApp.Password, rol = role,
        fechaIngreso = role == "Empleado" ? "2026-01-01" : null
    };

    private static object Supplier(string cuit) => new { nombre = "Proveedor", cuit };
    private static object Edit(string email, string cuit) => new { nombre = "Nombre", apellido = "Apellido", cuit, email };

    [Theory]
    [InlineData(Cuit)]
    [InlineData("20123456789")]
    [InlineData("20 12345678 9")]
    [InlineData("20\t12345678\r\n9")]
    public async Task Registration_rejects_equivalent_client_cuit_even_when_deleted(string repeated)
    {
        using var app = new AuthApp();
        using var client = await Admin(app);
        var response = await client.PostAsJsonAsync("/api/auth/register", Registration("one@example.test", Cuit));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var id = (await response.Content.ReadFromJsonAsync<UsuarioDto>())!.Id;
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync("/api/auth/register", Registration("two@example.test", repeated))).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync("/api/clientes/" + id)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync("/api/auth/register", Registration("two@example.test", repeated))).StatusCode);
        await app.InDatabase(async db =>
        {
            Assert.Equal(2, await db.Usuarios.IgnoreQueryFilters().CountAsync());
            Assert.Single(await db.Clientes.IgnoreQueryFilters().ToListAsync());
        });
    }

    [Theory]
    [InlineData("clientes")]
    [InlineData("usuarios")]
    public async Task Every_client_edit_route_rejects_duplicates_and_allows_own_cuit(string route)
    {
        using var app = new AuthApp();
        using var client = await Admin(app);
        var one = await client.PostAsJsonAsync("/api/auth/register", Registration("one@example.test", Cuit));
        var oneId = (await one.Content.ReadFromJsonAsync<UsuarioDto>())!.Id;
        var two = await client.PostAsJsonAsync("/api/auth/register", Registration("two@example.test", "27-12345678-0"));
        var twoId = (await two.Content.ReadFromJsonAsync<UsuarioDto>())!.Id;
        var url = "/api/" + route + "/" + twoId;
        Assert.Equal(HttpStatusCode.NoContent, (await client.PutAsJsonAsync(url, Edit("two@example.test", "27 12345678 0"))).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PutAsJsonAsync(url, Edit("two@example.test", "20123456789"))).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync("/api/clientes/" + oneId)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PutAsJsonAsync(url, Edit("two@example.test", "20 12345678 9"))).StatusCode);
        await app.InDatabase(async db => Assert.Equal("27 12345678 0", (await db.Usuarios.SingleAsync(u => u.Id == twoId)).CUIT));
    }

    [Theory]
    [InlineData(Cuit)]
    [InlineData("20123456789")]
    [InlineData("20 12345678 9")]
    [InlineData("20\t12345678\r\n9")]
    public async Task Supplier_create_and_edit_reject_equivalent_cuit_including_deleted(string repeated)
    {
        using var app = new AuthApp();
        using var client = await Admin(app);
        var first = await client.PostAsJsonAsync("/api/proveedores", Supplier(Cuit));
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        var id = (await first.Content.ReadFromJsonAsync<ProveedorResponse>())!.Id;
        var other = await client.PostAsJsonAsync("/api/proveedores", Supplier("27-12345678-0"));
        var otherId = (await other.Content.ReadFromJsonAsync<ProveedorResponse>())!.Id;
        Assert.Equal(HttpStatusCode.NoContent, (await client.PutAsJsonAsync("/api/proveedores/" + id, Supplier(repeated))).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync("/api/proveedores", Supplier(repeated))).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PutAsJsonAsync("/api/proveedores/" + otherId, Supplier(repeated))).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync("/api/proveedores/" + id)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync("/api/proveedores", Supplier(repeated))).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PutAsJsonAsync("/api/proveedores/" + otherId, Supplier(repeated))).StatusCode);
    }

    [Fact]
    public async Task Uniqueness_is_independent_between_suppliers_and_clients_and_does_not_include_other_roles()
    {
        using var app = new AuthApp();
        using var client = await Admin(app);
        // The initial administrator already has this same CUIT.
        Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync("/api/auth/register", Registration("customer@example.test", Cuit))).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync("/api/proveedores", Supplier("20123456789"))).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync("/api/auth/register", Registration("worker@example.test", Cuit, "Empleado"))).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync("/api/auth/register", Registration("worker2@example.test", Cuit, "Empleado"))).StatusCode);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("-- --")]
    [InlineData("123456789012345678901")]
    public async Task Empty_or_overlong_identifiers_are_rejected(string? cuit)
    {
        using var app = new AuthApp();
        using var client = await Admin(app);
        var registration = await client.PostAsJsonAsync("/api/auth/register", Registration("one@example.test", "123"));
        var id = (await registration.Content.ReadFromJsonAsync<UsuarioDto>())!.Id;
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/auth/register", Registration("invalid@example.test", cuit!))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/proveedores", Supplier(cuit!))).StatusCode);
        foreach (var route in new[] { "clientes", "usuarios" })
            Assert.Equal(HttpStatusCode.BadRequest, (await client.PutAsJsonAsync("/api/" + route + "/" + id, Edit("one@example.test", cuit!))).StatusCode);
    }

    [Fact]
    public async Task Database_enforces_client_cuit_without_application_prechecks()
    {
        using var app = new AuthApp();
        using var client = await Admin(app);
        Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync("/api/auth/register", Registration("one@example.test", Cuit))).StatusCode);
        // Different stored strings must still collide at the unique computed index.
        await Assert.ThrowsAsync<DbUpdateException>(() => app.InDatabase(async db =>
        {
            var user = new Usuario { Nombre = "Duplicado", Apellido = "Prueba", CUIT = "20123456789",
                Email = "bypass@example.test", Rol = Usuario.RolCliente };
            user.PasswordHash = new PasswordHasher<Usuario>().HashPassword(user, AuthApp.Password);
            user.Cliente = new Cliente { Usuario = user };
            db.Usuarios.Add(user);
            await db.SaveChangesAsync();
        }));
        await app.InDatabase(async db => Assert.Equal(2, await db.Usuarios.CountAsync()));
    }

    [Fact]
    public async Task Database_enforces_supplier_cuit_without_application_prechecks()
    {
        using var app = new AuthApp();
        using var client = await Admin(app);
        Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync("/api/proveedores", Supplier(Cuit))).StatusCode);
        await Assert.ThrowsAsync<DbUpdateException>(() => app.InDatabase(async db =>
        {
            db.Proveedores.Add(new Proveedor { Nombre = "Duplicado", CUIT = "20123456789" });
            await db.SaveChangesAsync();
        }));
        await app.InDatabase(async db => Assert.Equal(1, await db.Proveedores.CountAsync()));
    }
}
