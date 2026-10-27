using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using TPN_5_MOVIL.Data;
using TPN_5_MOVIL.Exceptions;
using TPN_5_MOVIL.Services;
using TPN_5_MOVIL.Security;
using TPN_5_MOVIL.DTOs;
using TPN_5_MOVIL.Entities;

var bootstrapMode = args.Contains("--crear-administrador", StringComparer.Ordinal);
var builder = WebApplication.CreateBuilder(args.Where(a => a != "--crear-administrador").ToArray());

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

var jwt = JwtSettings.Load(builder.Configuration);
builder.Services.AddSingleton(jwt);
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<AuditoriaService>();
builder.Services.AddScoped<SalidaService>();
builder.Services.AddScoped<IngresoService>();
builder.Services.AddScoped<ImagenProductoService>();
builder.Services.AddSingleton<IAlmacenImagenes, AlmacenImagenes>();

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.MapInboundClaims = false;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        RequireSignedTokens = true,
        RequireExpirationTime = true,
        ValidIssuer = jwt.Issuer,
        ValidAudience = jwt.Audience,
        IssuerSigningKey = jwt.SigningKey,
        ValidAlgorithms = new[] { SecurityAlgorithms.HmacSha256 },
        NameClaimType = "name",
        RoleClaimType = "role",
        ClockSkew = TimeSpan.FromSeconds(30)
    };
    options.Events = new JwtBearerEvents
    {
        OnTokenValidated = async context =>
        {
            var auth = context.HttpContext.RequestServices.GetRequiredService<AuthService>();
            if (context.Principal is null ||
                !await auth.ValidarTokenAsync(context.Principal, context.HttpContext.RequestAborted))
                context.Fail("Cuenta o perfil no disponible.");
        }
    };
});

builder.Services.AddAuthorization();

var loginRateLimit = LoginRateLimitSettings.Load(builder.Configuration);
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy(LoginRateLimitSettings.PolicyName, context =>
    {
        // Use the connection IP only. Forwarded headers must not create trusted identities.
        var address = context.Connection.RemoteIpAddress;
        if (address?.IsIPv4MappedToIPv6 == true) address = address.MapToIPv4();
        return RateLimitPartition.GetFixedWindowLimiter(address?.ToString() ?? "unknown", _ =>
            new FixedWindowRateLimiterOptions
            {
                PermitLimit = loginRateLimit.PermitLimit,
                Window = TimeSpan.FromSeconds(loginRateLimit.WindowSeconds),
                QueueLimit = 0,
                AutoReplenishment = true
            });
    });
    options.OnRejected = async (context, cancellationToken) =>
    {
        var retry = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var remaining)
            ? remaining : TimeSpan.FromSeconds(loginRateLimit.WindowSeconds);
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        context.HttpContext.Response.Headers.RetryAfter =
            Math.Max(1, (int)Math.Ceiling(retry.TotalSeconds)).ToString(CultureInfo.InvariantCulture);
        await context.HttpContext.Response.WriteAsJsonAsync(new
        {
            error = "Demasiados intentos de inicio de sesión. Vuelva a intentarlo más tarde."
        }, cancellationToken);
    };
});

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});

builder.Services.AddControllers().AddJsonOptions(o =>
{
    o.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "TPN5 - API de Inventario",
        Version = "v1",
        Description = "API REST para gestión de productos, proveedores, clientes, ingresos y ventas de inventario."
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Ingresar el token JWT con formato: Bearer {token}",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// Explicit bootstrap against an already migrated database. No automatic seed or migration.
if (bootstrapMode)
{
    using var scope = app.Services.CreateScope();
    var config = app.Configuration;
    var auth = scope.ServiceProvider.GetRequiredService<AuthService>();
    await auth.CrearAdministradorInicialAsync(new RegisterRequest
    {
        Nombre = config["Bootstrap:Nombre"] ?? string.Empty,
        Apellido = config["Bootstrap:Apellido"] ?? string.Empty,
        CUIT = config["Bootstrap:CUIT"] ?? string.Empty,
        Email = config["Bootstrap:Email"] ?? string.Empty,
        Password = config["Bootstrap:Password"] ?? string.Empty,
        Rol = Usuario.RolAdministrador
    });
    Console.WriteLine("Administrador inicial creado. No se inició el servidor HTTP.");
    return;
}

var webRoot = app.Environment.WebRootPath ?? Path.Combine(app.Environment.ContentRootPath, "wwwroot");
Directory.CreateDirectory(webRoot);
app.Environment.WebRootPath = webRoot;
var webFiles = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(webRoot);
app.Environment.WebRootFileProvider = webFiles;
app.Lifetime.ApplicationStopped.Register(webFiles.Dispose);
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = context => context.Context.Response.Headers["X-Content-Type-Options"] = "nosniff"
});

app.UseRouting();
app.UseCors();

app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (BadHttpRequestException ex)
    {
        context.Response.StatusCode = ex.StatusCode;
        await context.Response.WriteAsJsonAsync(new { error = "Petición de archivo inválida o demasiado grande." });
    }
    catch (ApiException ex)
    {
        context.Response.StatusCode = ex.StatusCode;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new { error = ex.Message });
    }
    catch (Exception ex)
    {
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new { error = "Error interno del servidor." });

        if (app.Environment.IsDevelopment())
            Console.WriteLine(ex);
    }
});

app.UseSwagger();
app.UseSwaggerUI();

// Endpoint routing must run before the endpoint-specific login limiter.
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
// Required for WebApplicationFactory integration tests.
public partial class Program { }
