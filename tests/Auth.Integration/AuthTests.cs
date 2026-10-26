using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using TPN_5_MOVIL.Data;
using TPN_5_MOVIL.DTOs;
using TPN_5_MOVIL.Entities;
using TPN_5_MOVIL.Exceptions;
using TPN_5_MOVIL.Security;
using TPN_5_MOVIL.Services;
using Xunit;

namespace Auth.Integration;

public sealed class AuthApp : WebApplicationFactory<Program>
{
    public const string Password = "Only-Test-Password!42";
    public const string Key = "Isolated-test-key-at-least-thirty-two-bytes-2026";
    private readonly SqliteConnection? connection;
    private readonly string? sqlConnection;

    public AuthApp()
    {
        var template = Environment.GetEnvironmentVariable("AUTH_TEST_SQLSERVER_CONNECTION");
        if (string.IsNullOrWhiteSpace(template))
        {
            connection = new SqliteConnection("Data Source=:memory:");
            connection.Open();
        }
        else
        {
            // Each factory creates its own database. Run only against a disposable test server.
            var builder = new SqlConnectionStringBuilder(template)
            {
                InitialCatalog = "TPN5_AuthTest_" + Guid.NewGuid().ToString("N")
            };
            sqlConnection = builder.ConnectionString;
        }
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("Jwt:Issuer", "auth-tests");
        builder.UseSetting("Jwt:Audience", "auth-tests");
        builder.UseSetting("Jwt:Key", Key);
        builder.UseSetting("Jwt:ExpireMinutes", "10");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<AppDbContext>();
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();
            services.AddDbContext<AppDbContext>(o =>
            {
                if (sqlConnection is null) o.UseSqlite(connection!);
                else o.UseSqlServer(sqlConnection);
            });
        });
    }

    public async Task InitializeAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        if (db.Database.IsSqlServer()) await db.Database.MigrateAsync();
        else await db.Database.EnsureCreatedAsync(); // SQLite cannot execute SQL Server migrations.
        await scope.ServiceProvider.GetRequiredService<AuthService>().CrearAdministradorInicialAsync(
            Registration(Usuario.RolAdministrador, "admin@example.test"));
    }

    public static RegisterRequest Registration(string role, string email = "person@example.test") => new()
    {
        Nombre = "Nombre", Apellido = "Prueba", CUIT = "20-12345678-9", Email = email,
        Password = Password, Rol = role,
        FechaIngreso = role == Usuario.RolEmpleado ? new DateOnly(2026, 1, 1) : null,
        Telefono = role == Usuario.RolCliente ? "1234" : null
    };

    public async Task InDatabase(Func<AppDbContext, Task> action)
    {
        using var scope = Services.CreateScope();
        await action(scope.ServiceProvider.GetRequiredService<AppDbContext>());
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing) connection?.Dispose();
    }
}

public class AuthTests
{
    private static async Task<LoginResponse> Login(HttpClient client, string email = "admin@example.test", string password = AuthApp.Password)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest { Email = email, Password = password });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<LoginResponse>())!;
    }

    private static void UseToken(HttpClient client, string token) =>
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    private static async Task<HttpClient> AdminClient(AuthApp app)
    {
        var client = app.CreateClient();
        await app.InitializeAsync();
        UseToken(client, (await Login(client)).Token);
        return client;
    }

    [Fact]
    public async Task Anonymous_cannot_read_identity_or_register()
    {
        using var app = new AuthApp();
        using var client = app.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/auth/me")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/auth/register", AuthApp.Registration("Cliente"))).StatusCode);
    }

    [Theory]
    [InlineData("Administrador")]
    [InlineData("Empleado")]
    [InlineData("Cliente")]
    public async Task Registration_creates_exact_profile_and_can_login(string role)
    {
        using var app = new AuthApp();
        using var client = await AdminClient(app);
        var registration = await client.PostAsJsonAsync("/api/auth/register", AuthApp.Registration(role, "PERSON@example.test"));
        Assert.Equal(HttpStatusCode.Created, registration.StatusCode);
        Assert.NotNull(registration.Headers.Location);
        var dto = (await registration.Content.ReadFromJsonAsync<UsuarioDto>())!;
        Assert.Equal("person@example.test", dto.Email);
        Assert.DoesNotContain("password", (await registration.Content.ReadAsStringAsync()).ToLowerInvariant());
        await app.InDatabase(async db =>
        {
            var user = await db.Usuarios.Include(u => u.Empleado).Include(u => u.Cliente).SingleAsync(u => u.Id == dto.Id);
            Assert.NotEqual(AuthApp.Password, user.PasswordHash);
            Assert.Equal(role == "Empleado", user.Empleado is not null);
            Assert.Equal(role == "Cliente", user.Cliente is not null);
            if (user.Empleado is not null) Assert.Equal(user.Id, user.Empleado.Id);
            if (user.Cliente is not null) Assert.Equal(user.Id, user.Cliente.Id);
        });
        var login = await Login(client, "PERSON@example.test");
        UseToken(client, login.Token);
        var me = await client.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.OK, me.StatusCode);
        Assert.Equal(dto.Id, (await me.Content.ReadFromJsonAsync<UsuarioDto>())!.Id);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(login.Token);
        Assert.Equal(dto.Id.ToString(), jwt.Subject);
        Assert.Equal(role, jwt.Claims.Single(c => c.Type == "role").Value);
        Assert.Equal(login.ExpiresAt, jwt.ValidTo);
    }

    [Theory]
    [InlineData("Empleado")]
    [InlineData("Cliente")]
    public async Task Non_admin_cannot_assign_roles(string role)
    {
        using var app = new AuthApp();
        using var client = await AdminClient(app);
        Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync("/api/auth/register", AuthApp.Registration(role))).StatusCode);
        UseToken(client, (await Login(client, "person@example.test")).Token);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync("/api/auth/register", AuthApp.Registration("Administrador", "attack@example.test"))).StatusCode);
    }

    [Theory]
    [InlineData("Admin", null, null)]
    [InlineData("Empleado", null, null)]
    [InlineData("Empleado", "2026-01-01", "123")]
    [InlineData("Cliente", "2026-01-01", null)]
    [InlineData("Administrador", null, "123")]
    public async Task Invalid_role_or_mixed_profile_leaves_no_account(string role, string? date, string? phone)
    {
        using var app = new AuthApp();
        using var client = await AdminClient(app);
        var request = new RegisterRequest
        {
            Nombre = "Prueba", Apellido = "Persona", CUIT = "123", Email = "invalid@example.test",
            Password = AuthApp.Password, Rol = role,
            FechaIngreso = date is null ? null : DateOnly.Parse(date), Telefono = phone
        };
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/auth/register", request)).StatusCode);
        await app.InDatabase(async db => Assert.Equal(1, await db.Usuarios.CountAsync()));
    }

    [Fact]
    public async Task Invalid_registration_fields_are_rejected_by_MVC()
    {
        using var app = new AuthApp();
        using var client = await AdminClient(app);
        var response = await client.PostAsJsonAsync("/api/auth/register", new { nombre = "", apellido = "A", cuit = "1", email = "not-an-email", password = "short", rol = "Cliente" });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await app.InDatabase(async db => Assert.Equal(1, await db.Usuarios.CountAsync()));
    }

    [Fact]
    public async Task Wrong_password_unknown_account_and_deleted_account_return_401()
    {
        using var app = new AuthApp();
        using var client = await AdminClient(app);
        foreach (var email in new[] { "admin@example.test", "unknown@example.test" })
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/auth/login", new LoginRequest { Email = email, Password = "wrong-password" })).StatusCode);
        await app.InDatabase(db => db.Usuarios.ExecuteUpdateAsync(s => s.SetProperty(u => u.Eliminado, true)));
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/auth/login", new LoginRequest { Email = "admin@example.test", Password = AuthApp.Password })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/auth/me")).StatusCode);
    }

    [Fact]
    public async Task Duplicate_deleted_email_is_not_reusable()
    {
        using var app = new AuthApp();
        using var client = await AdminClient(app);
        Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync("/api/auth/register", AuthApp.Registration("Cliente"))).StatusCode);
        await app.InDatabase(db => db.Usuarios.Where(u => u.Email == "person@example.test").ExecuteUpdateAsync(s => s.SetProperty(u => u.Eliminado, true)));
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync("/api/auth/register", AuthApp.Registration("Cliente", "PERSON@example.test"))).StatusCode);
    }

    [Fact]
    public async Task Password_change_revokes_previously_issued_token()
    {
        using var app = new AuthApp();
        using var client = await AdminClient(app);
        const string replacement = "Replacement-test-password!42";
        await app.InDatabase(async db =>
        {
            var user = await db.Usuarios.SingleAsync();
            user.PasswordHash = new PasswordHasher<Usuario>().HashPassword(user, replacement);
            await db.SaveChangesAsync();
        });
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/auth/me")).StatusCode);
        UseToken(client, (await Login(client, password: replacement)).Token);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/auth/me")).StatusCode);
    }

    [Fact]
    public async Task Existing_password_update_route_rejects_short_password()
    {
        using var app = new AuthApp();
        using var client = await AdminClient(app);
        var id = (await Login(client)).Usuario.Id;
        var response = await client.PutAsJsonAsync("/api/usuarios/" + id, new
        {
            nombre = "Nombre", apellido = "Prueba", cuit = "20-12345678-9",
            email = "admin@example.test", password = "short"
        });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/auth/me")).StatusCode);
        await Login(client);
    }

    [Fact]
    public async Task Account_with_both_profiles_cannot_login_or_use_existing_token()
    {
        using var app = new AuthApp();
        using var client = await AdminClient(app);
        Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync("/api/auth/register", AuthApp.Registration("Cliente"))).StatusCode);
        UseToken(client, (await Login(client, "person@example.test")).Token);
        await app.InDatabase(async db =>
        {
            var id = await db.Usuarios.Where(u => u.Email == "person@example.test").Select(u => u.Id).SingleAsync();
            db.Empleados.Add(new Empleado { Id = id, FechaIngreso = new DateOnly(2026, 1, 1) });
            await db.SaveChangesAsync();
        });
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/auth/me")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/auth/login", new LoginRequest { Email = "person@example.test", Password = AuthApp.Password })).StatusCode);
    }

    [Fact]
    public async Task Profile_insert_failure_rolls_back_principal_insert()
    {
        using var app = new AuthApp();
        using var client = await AdminClient(app);
        await app.InDatabase(db => db.Database.ExecuteSqlRawAsync(db.Database.IsSqlServer()
            ? "ALTER TABLE Clientes ADD CONSTRAINT CK_RejectTestClient CHECK (Id < 0);"
            : "CREATE TRIGGER reject_client BEFORE INSERT ON Clientes BEGIN SELECT RAISE(ABORT, 'injected profile failure'); END;"));
        Assert.Equal(HttpStatusCode.InternalServerError, (await client.PostAsJsonAsync("/api/auth/register", AuthApp.Registration("Cliente"))).StatusCode);
        await app.InDatabase(async db =>
        {
            Assert.Equal(1, await db.Usuarios.CountAsync());
            Assert.Equal(0, await db.Clientes.CountAsync());
        });
    }

    [Fact]
    public async Task Initial_admin_cannot_be_created_twice_even_if_deleted()
    {
        using var app = new AuthApp();
        using var client = await AdminClient(app);
        await app.InDatabase(db => db.Usuarios.ExecuteUpdateAsync(s => s.SetProperty(u => u.Eliminado, true)));
        using var scope = app.Services.CreateScope();
        var error = await Assert.ThrowsAsync<ApiException>(() => scope.ServiceProvider.GetRequiredService<AuthService>()
            .CrearAdministradorInicialAsync(AuthApp.Registration("Administrador", "second@example.test")));
        Assert.Equal(409, error.StatusCode);
    }

    [Theory]
    [InlineData("expired")]
    [InlineData("audience")]
    [InlineData("signature")]
    [InlineData("role")]
    public async Task Invalid_token_is_rejected(string mutation)
    {
        using var app = new AuthApp();
        using var client = await AdminClient(app);
        var issued = new JwtSecurityTokenHandler().ReadJwtToken((await Login(client)).Token);
        var claims = issued.Claims.Where(c => c.Type is not ("exp" or "nbf" or "iss" or "aud" or "role")).ToList();
        claims.Add(new Claim("role", mutation == "role" ? "Empleado" : "Administrador"));
        var key = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(mutation == "signature" ? "different-test-signing-key-at-least-32-bytes" : AuthApp.Key));
        var token = new JwtSecurityToken("auth-tests", mutation == "audience" ? "wrong" : "auth-tests", claims,
            DateTime.UtcNow.AddMinutes(-10), mutation == "expired" ? DateTime.UtcNow.AddMinutes(-2) : DateTime.UtcNow.AddMinutes(2),
            new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
        UseToken(client, new JwtSecurityTokenHandler().WriteToken(token));
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/auth/me")).StatusCode);
    }

    [Fact]
    public async Task Cors_preflight_and_swagger_work_without_authentication()
    {
        using var app = new AuthApp();
        using var client = app.CreateClient();
        var doc = await client.GetFromJsonAsync<JsonElement>("/swagger/v1/swagger.json");
        Assert.True(doc.GetProperty("components").GetProperty("schemas").GetProperty("RegisterRequest").GetProperty("properties").TryGetProperty("fechaIngreso", out _));
        using var req = new HttpRequestMessage(HttpMethod.Options, "/api/auth/register");
        req.Headers.Add("Origin", "http://example.invalid");
        req.Headers.Add("Access-Control-Request-Method", "POST");
        var response = await client.SendAsync(req);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal("*", response.Headers.GetValues("Access-Control-Allow-Origin").Single());
    }

    [Fact]
    public void Missing_or_weak_JWT_configuration_fails_early()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        { ["Jwt:Key"] = "short", ["Jwt:Issuer"] = "test", ["Jwt:Audience"] = "test" }).Build();
        Assert.Throws<InvalidOperationException>(() => JwtSettings.Load(config));
    }
}
