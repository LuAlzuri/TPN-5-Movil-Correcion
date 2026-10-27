using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using TPN_5_MOVIL.Data;
using TPN_5_MOVIL.DTOs;
using TPN_5_MOVIL.Entities;
using TPN_5_MOVIL.Services;
using Xunit;

namespace TPN5.Integration;

internal sealed class ApiFactory(string connectionString, string webRoot, bool loginLimitScenario)
    : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseWebRoot(webRoot);
        builder.UseSetting("ErrorLog:Path", Path.Combine(webRoot, "..", "logs", "errores-.log"));
        builder.UseSetting("Jwt:Issuer", "tpn5-tests");
        builder.UseSetting("Jwt:Audience", "tpn5-tests");
        builder.UseSetting("Jwt:Key", Convert.ToBase64String(RandomNumberGenerator.GetBytes(48)));
        builder.UseSetting("Jwt:ExpireMinutes", "10");
        // Keep setup logins from exhausting the quota in unrelated scenarios.
        builder.UseSetting("LoginRateLimit:PermitLimit", loginLimitScenario ? "5" : "100");
        builder.UseSetting("LoginRateLimit:WindowSeconds", loginLimitScenario ? "5" : "60");
        builder.ConfigureLogging(logging => logging.SetMinimumLevel(LogLevel.Error));
        builder.ConfigureServices(services =>
        {
            // Always replace application bindings. Never connect to user-configured databases.
            services.RemoveAll<AppDbContext>();
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();
            services.AddDbContext<AppDbContext>(options => options.UseSqlServer(connectionString));
        });
    }
}

public sealed record TestAccount(int Id, string Email, string Token);

public sealed class TestApplication : IAsyncDisposable
{
    private readonly string connectionString;
    private readonly ApiFactory factory;
    private readonly string root;
    private int sequence;
    public HttpClient Http { get; }
    public string Password { get; } = Convert.ToBase64String(RandomNumberGenerator.GetBytes(24)) + "aA1!";
    public TestAccount Admin { get; private set; } = null!;
    public string Uploads => Path.Combine(root, "wwwroot", "uploads");
    public string Logs => Path.Combine(root, "logs");

    private TestApplication(string serverConnection, bool loginLimitScenario)
    {
        var builder = new SqlConnectionStringBuilder(serverConnection)
        {
            InitialCatalog = "TPN5_Test_" + Guid.NewGuid().ToString("N")
        };
        connectionString = builder.ConnectionString;
        root = Path.Combine(Path.GetTempPath(), "tpn5-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        factory = new ApiFactory(connectionString, Path.Combine(root, "wwwroot"), loginLimitScenario);
        try { Http = factory.CreateClient(); }
        catch
        {
            factory.Dispose();
            Directory.Delete(root, recursive: true);
            throw;
        }
    }

    internal static async Task<TestApplication> CreateAsync(string serverConnection, bool loginLimitScenario)
    {
        var app = new TestApplication(serverConnection, loginLimitScenario);
        try
        {
            using var scope = app.factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.Database.MigrateAsync();
            var user = await scope.ServiceProvider.GetRequiredService<AuthService>().CrearAdministradorInicialAsync(new RegisterRequest
            {
                Nombre = "Admin", Apellido = "Prueba", CUIT = "1", Email = "admin@example.test",
                Password = app.Password, Rol = Usuario.RolAdministrador
            });
            // No login here: the rate-limit scenario must start with all five permits.
            app.Admin = new TestAccount(user.Id, user.Email, string.Empty);
            return app;
        }
        catch
        {
            await app.DisposeAsync();
            throw;
        }
    }

    public async Task<TestAccount> LoginAdminAsync()
    {
        Admin = await LoginAsync(Admin.Email);
        return Admin;
    }

    public async Task<TestAccount> LoginAsync(string email)
    {
        using var response = await Http.PostAsJsonAsync("/api/auth/login", new { email, password = Password });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var data = (await response.Content.ReadFromJsonAsync<LoginResponse>())!;
        return new TestAccount(data.Usuario.Id, data.Usuario.Email, data.Token);
    }

    public async Task<HttpResponseMessage> SendAsync(HttpMethod method, string path,
        TestAccount? actor = null, object? body = null)
    {
        using var request = new HttpRequestMessage(method, path);
        if (actor is not null) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", actor.Token);
        if (body is not null) request.Content = JsonContent.Create(body);
        return await Http.SendAsync(request);
    }

    public async Task<T> JsonAsync<T>(HttpMethod method, string path, TestAccount actor,
        object? body = null, HttpStatusCode expected = HttpStatusCode.OK)
    {
        using var response = await SendAsync(method, path, actor, body);
        Assert.Equal(expected, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<T>())!;
    }

    public async Task StatusAsync(HttpMethod method, string path, HttpStatusCode expected,
        TestAccount? actor = null, object? body = null)
    {
        using var response = await SendAsync(method, path, actor, body);
        Assert.Equal(expected, response.StatusCode);
    }

    public async Task<TestAccount> RegisterAsync(string role)
    {
        var n = Interlocked.Increment(ref sequence);
        var email = $"account{n}@example.test";
        await StatusAsync(HttpMethod.Post, "/api/auth/register", HttpStatusCode.Created, Admin, new
        {
            nombre = "Cuenta", apellido = "Prueba", cuit = (10000 + n).ToString(), email,
            password = Password, rol = role,
            fechaIngreso = role == Usuario.RolEmpleado ? "2026-01-01" : null
        });
        return await LoginAsync(email);
    }

    public async Task DatabaseAsync(Func<AppDbContext, Task> action)
    {
        using var scope = factory.Services.CreateScope();
        await action(scope.ServiceProvider.GetRequiredService<AppDbContext>());
    }

    public async ValueTask DisposeAsync()
    {
        Http.Dispose();
        try { await factory.DisposeAsync(); }
        finally
        {
            try
            {
                // This name and connection are generated solely from our owned container.
                using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
                    .UseSqlServer(connectionString).Options);
                await db.Database.EnsureDeletedAsync();
            }
            finally
            {
                if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
            }
        }
    }
}
