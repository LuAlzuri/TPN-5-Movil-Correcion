using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TPN_5_MOVIL.DTOs;
using TPN_5_MOVIL.Entities;
using Xunit;

namespace TPN5.Integration;

[Collection(SqlServerCollection.Name)]
public sealed class ActualizacionUsuariosTests(SqlServerFixture sql)
{
    private static Dictionary<string, object?> Datos(UsuarioDto user) => new()
    {
        ["nombre"] = user.Nombre, ["apellido"] = user.Apellido,
        ["cuit"] = user.CUIT, ["email"] = user.Email
    };

    private static Task<UsuarioDto> UsuarioAsync(TestApplication app, int id) =>
        app.JsonAsync<UsuarioDto>(HttpMethod.Get, $"/api/usuarios/{id}", app.Admin);

    [Fact]
    public async Task Error_inesperado_se_registra_sin_exponer_credenciales_ni_archivos()
    {
        var app = await sql.CreateApplicationAsync();
        await app.LoginAdminAsync();
        await app.DatabaseAsync(db => db.Database.ExecuteSqlRawAsync(
            "ALTER TABLE Clientes ADD CONSTRAINT CK_TestErrorLog CHECK (Id < 0);"));
        using var response = await app.SendAsync(HttpMethod.Post, "/api/auth/register", app.Admin,
            new { nombre = "Cliente", apellido = "Log", cuit = "123", email = "log@example.test", password = app.Password, rol = "Cliente" });
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Error interno del servidor.", error.GetProperty("error").GetString());
        var files = Directory.GetFiles(app.Logs, "*.log");
        Assert.NotEmpty(files);
        var content = string.Join("\n", files.Select(path =>
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }));
        await app.StatusAsync(HttpMethod.Get, "/logs/" + Path.GetFileName(files[0]), HttpStatusCode.NotFound);
        await app.DisposeAsync();
        Assert.DoesNotContain(app.Password, content);
        Assert.DoesNotContain(app.Admin.Token, content);
        using var entry = content.Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(line => JsonDocument.Parse(line)).First(d => d.RootElement.GetProperty("Properties").TryGetProperty("Method", out _));
        var properties = entry.RootElement.GetProperty("Properties");
        Assert.Equal("POST", properties.GetProperty("Method").GetString());
        Assert.Equal("/api/auth/register", properties.GetProperty("Path").GetString());
        Assert.Equal(app.Admin.Id.ToString(), properties.GetProperty("UsuarioId").GetString());
        Assert.False(string.IsNullOrWhiteSpace(properties.GetProperty("TraceId").GetString()));
        Assert.Contains("SqlException", entry.RootElement.GetProperty("Exception").GetString());
    }

    [Fact]
    public async Task Edicion_propia_normaliza_y_conserva_unicidad_y_rechazo_de_campos_ajenos()
    {
        await using var app = await sql.CreateApplicationAsync();
        await app.LoginAdminAsync();
        var client = await app.RegisterAsync(Usuario.RolCliente);
        var employee = await app.RegisterAsync(Usuario.RolEmpleado);
        var removed = await app.RegisterAsync(Usuario.RolCliente);
        var reserved = await UsuarioAsync(app, removed.Id);
        await app.StatusAsync(HttpMethod.Delete, $"/api/clientes/{removed.Id}", HttpStatusCode.NoContent, app.Admin);
        foreach (var (path, id, isEmployee) in new[]
        {
            ($"/api/usuarios/{app.Admin.Id}", app.Admin.Id, false),
            ($"/api/clientes/{client.Id}", client.Id, false),
            ($"/api/empleados/{employee.Id}", employee.Id, true)
        })
        {
            var before = await UsuarioAsync(app, id);
            var body = Datos(before);
            if (isEmployee) body["fechaIngreso"] = "2026-01-01";
            body["nombre"] = " Editado ";
            body["apellido"] = " Normalizado ";
            body["cuit"] = $" {before.CUIT} ";
            body["email"] = before.Email.ToUpperInvariant();
            await app.StatusAsync(HttpMethod.Put, path, HttpStatusCode.NoContent, app.Admin, body);
            var expected = before with { Nombre = "Editado", Apellido = "Normalizado" };
            Assert.Equal(expected, await UsuarioAsync(app, id));
            body["email"] = reserved.Email.ToUpperInvariant();
            await app.StatusAsync(HttpMethod.Put, path, HttpStatusCode.Conflict, app.Admin, body);
            body["email"] = before.Email;
            foreach (var field in new[] { "rol", "id" })
            {
                body[field] = field == "rol" ? Usuario.RolAdministrador : id;
                await app.StatusAsync(HttpMethod.Put, path, HttpStatusCode.BadRequest, app.Admin, body);
                body.Remove(field);
            }
            Assert.Equal(expected, await UsuarioAsync(app, id));
        }
        var currentClient = await UsuarioAsync(app, client.Id);
        var duplicateCuit = Datos(currentClient);
        duplicateCuit["cuit"] = reserved.CUIT;
        foreach (var path in new[] { $"/api/clientes/{client.Id}", $"/api/usuarios/{client.Id}" })
            await app.StatusAsync(HttpMethod.Put, path, HttpStatusCode.Conflict, app.Admin, duplicateCuit);
        Assert.Equal(currentClient, await UsuarioAsync(app, client.Id));
        await app.DatabaseAsync(async db =>
        {
            Assert.True(await db.Clientes.AnyAsync(c => c.Id == client.Id));
            Assert.False(await db.Empleados.AnyAsync(e => e.Id == client.Id));
            Assert.True(await db.Empleados.AnyAsync(e => e.Id == employee.Id));
            Assert.False(await db.Clientes.AnyAsync(c => c.Id == employee.Id));
        });
    }

    [Fact]
    public async Task Password_omitida_conserva_hash_y_password_nueva_revoca_token()
    {
        await using var app = await sql.CreateApplicationAsync();
        await app.LoginAdminAsync();
        var client = await app.RegisterAsync(Usuario.RolCliente);
        var user = await UsuarioAsync(app, client.Id);
        string? hash = null;
        await app.DatabaseAsync(async db => hash = (await db.Usuarios.SingleAsync(u => u.Id == client.Id)).PasswordHash);
        var body = Datos(user);
        body["nombre"] = " Editado ";
        body["email"] = $" {user.Email.ToUpperInvariant()} ";
        await app.StatusAsync(HttpMethod.Put, $"/api/usuarios/{client.Id}", HttpStatusCode.NoContent, app.Admin, body);
        await app.DatabaseAsync(async db => Assert.Equal(hash, (await db.Usuarios.SingleAsync(u => u.Id == client.Id)).PasswordHash));
        await app.StatusAsync(HttpMethod.Get, "/api/auth/me", HttpStatusCode.OK, client);
        const string newPassword = "OtraClave_Segura2026!";
        body["password"] = newPassword;
        await app.StatusAsync(HttpMethod.Put, $"/api/usuarios/{client.Id}", HttpStatusCode.NoContent, app.Admin, body);
        await app.StatusAsync(HttpMethod.Get, "/api/auth/me", HttpStatusCode.Unauthorized, client);
        await app.StatusAsync(HttpMethod.Post, "/api/auth/login", HttpStatusCode.Unauthorized,
            body: new { email = user.Email, password = app.Password });
        using var response = await app.SendAsync(HttpMethod.Post, "/api/auth/login",
            body: new { email = user.Email, password = newPassword });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var login = (await response.Content.ReadFromJsonAsync<LoginResponse>())!;
        Assert.Equal(client.Id, login.Usuario.Id);
        Assert.Equal(Usuario.RolCliente, login.Usuario.Rol);
        Assert.Equal("Editado", login.Usuario.Nombre);
        await app.StatusAsync(HttpMethod.Get, "/api/auth/me", HttpStatusCode.OK,
            new TestAccount(client.Id, user.Email, login.Token));
        await app.DatabaseAsync(async db => Assert.NotEqual(hash, (await db.Usuarios.SingleAsync(u => u.Id == client.Id)).PasswordHash));
    }

    [Fact]
    public async Task Fallo_de_actualizacion_de_perfil_revierte_datos_de_la_cuenta()
    {
        await using var app = await sql.CreateApplicationAsync();
        await app.LoginAdminAsync();
        var client = await app.RegisterAsync(Usuario.RolCliente);
        var employee = await app.RegisterAsync(Usuario.RolEmpleado);
        var clientBefore = await app.JsonAsync<ClienteResponse>(HttpMethod.Get, $"/api/clientes/{client.Id}", app.Admin);
        var employeeBefore = await app.JsonAsync<EmpleadoResponse>(HttpMethod.Get, $"/api/empleados/{employee.Id}", app.Admin);
        await app.DatabaseAsync(async db =>
        {
            await db.Database.ExecuteSqlRawAsync("ALTER TABLE Clientes ADD CONSTRAINT CK_TestContact CHECK (Telefono IS NULL OR Telefono <> 'rechazado');");
            await db.Database.ExecuteSqlRawAsync("ALTER TABLE Empleados ADD CONSTRAINT CK_TestDate CHECK (FechaIngreso <> '2026-02-02');");
        });
        foreach (var (path, id, field, value) in new[]
        {
            ($"/api/clientes/{client.Id}", client.Id, "telefono", "rechazado"),
            ($"/api/empleados/{employee.Id}", employee.Id, "fechaIngreso", "2026-02-02")
        })
        {
            var before = await UsuarioAsync(app, id);
            var body = Datos(before);
            body["nombre"] = "Cambio que debe revertirse";
            body["email"] = $"rollback{id}@example.test";
            body[field] = value;
            await app.StatusAsync(HttpMethod.Put, path, HttpStatusCode.InternalServerError, app.Admin, body);
            Assert.Equal(before, await UsuarioAsync(app, id));
        }
        Assert.Equal(clientBefore, await app.JsonAsync<ClienteResponse>(HttpMethod.Get, $"/api/clientes/{client.Id}", app.Admin));
        Assert.Equal(employeeBefore, await app.JsonAsync<EmpleadoResponse>(HttpMethod.Get, $"/api/empleados/{employee.Id}", app.Admin));
    }
}
