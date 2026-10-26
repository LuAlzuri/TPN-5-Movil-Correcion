using System.Data;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using TPN_5_MOVIL.Data;
using TPN_5_MOVIL.DTOs;
using TPN_5_MOVIL.Entities;
using TPN_5_MOVIL.Exceptions;
using TPN_5_MOVIL.Security;

namespace TPN_5_MOVIL.Services;

public class AuthService(AppDbContext db, JwtSettings jwt)
{
    private readonly PasswordHasher<Usuario> hasher = new();
    // Valid dummy hash makes unknown accounts perform the same expensive password check.
    private static readonly string DummyHash = new PasswordHasher<Usuario>()
        .HashPassword(new Usuario(), Convert.ToHexString(RandomNumberGenerator.GetBytes(32)));

    private IQueryable<Usuario> Cuentas => db.Usuarios.AsNoTracking()
        .Include(u => u.Empleado).Include(u => u.Cliente);

    public async Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var email = request.Email.Trim().ToUpperInvariant();
        var usuario = await Cuentas.SingleOrDefaultAsync(u => u.Email.ToUpper() == email, cancellationToken);
        var result = hasher.VerifyHashedPassword(usuario ?? new Usuario(), usuario?.PasswordHash ?? DummyHash, request.Password);
        if (usuario is null || result == PasswordVerificationResult.Failed || !PerfilValido(usuario))
            throw new ApiException("Credenciales inválidas o cuenta no disponible.", StatusCodes.Status401Unauthorized);

        if (result == PasswordVerificationResult.SuccessRehashNeeded)
        {
            // Conditional update prevents overwriting a concurrent password change or deletion.
            var replacement = hasher.HashPassword(usuario, request.Password);
            var updated = await db.Usuarios.Where(u => u.Id == usuario.Id && u.PasswordHash == usuario.PasswordHash)
                .ExecuteUpdateAsync(s => s.SetProperty(u => u.PasswordHash, replacement), cancellationToken);
            if (updated != 1)
                throw new ApiException("La cuenta cambió; vuelva a iniciar sesión.", StatusCodes.Status401Unauthorized);
            usuario.PasswordHash = replacement;
        }

        var now = DateTimeOffset.FromUnixTimeSeconds(DateTimeOffset.UtcNow.ToUnixTimeSeconds()).UtcDateTime;
        var expires = now.AddMinutes(jwt.ExpireMinutes);
        var claims = new[]
        {
            new Claim("sub", usuario.Id.ToString(CultureInfo.InvariantCulture)),
            new Claim("name", usuario.Nombre + " " + usuario.Apellido),
            new Claim("email", usuario.Email),
            new Claim("role", usuario.Rol),
            new Claim("auth_version", CredentialVersion(usuario)),
            new Claim("jti", Guid.NewGuid().ToString("N"))
        };
        var token = new JwtSecurityToken(jwt.Issuer, jwt.Audience, claims, now, expires,
            new SigningCredentials(jwt.SigningKey, SecurityAlgorithms.HmacSha256));
        return new LoginResponse(new JwtSecurityTokenHandler().WriteToken(token), expires, ToDto(usuario));
    }

    public async Task<UsuarioDto> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        ValidateRegistration(request);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var usuario = await CreateAccountAsync(request, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return ToDto(usuario);
    }

    // Explicit CLI operation only; never called automatically during API startup.
    public async Task<UsuarioDto> CrearAdministradorInicialAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Rol != Usuario.RolAdministrador)
            throw new ApiException("El usuario inicial debe ser Administrador.");
        ValidateRegistration(request);
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        if (await db.Usuarios.IgnoreQueryFilters().AnyAsync(u => u.Rol == Usuario.RolAdministrador, cancellationToken))
            throw new ApiException("Ya existe una cuenta de administrador; use el registro autenticado.", StatusCodes.Status409Conflict);
        var usuario = await CreateAccountAsync(request, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return ToDto(usuario);
    }

    private async Task<Usuario> CreateAccountAsync(RegisterRequest request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        if (await db.Usuarios.IgnoreQueryFilters().AnyAsync(u => u.Email.ToUpper() == email.ToUpperInvariant(), cancellationToken))
            throw new ApiException("Ya existe una cuenta con ese email.", StatusCodes.Status409Conflict);
        if (request.Rol == Usuario.RolCliente)
            await CuitUnico.ValidarClienteAsync(db, request.CUIT, cancellationToken: cancellationToken);
        var usuario = new Usuario
        {
            Nombre = request.Nombre.Trim(), Apellido = request.Apellido.Trim(), CUIT = request.CUIT.Trim(),
            Email = email, Rol = request.Rol
        };
        usuario.PasswordHash = hasher.HashPassword(usuario, request.Password);
        if (usuario.Rol == Usuario.RolEmpleado)
            usuario.Empleado = new Empleado { Usuario = usuario, FechaIngreso = request.FechaIngreso!.Value };
        else if (usuario.Rol == Usuario.RolCliente)
            usuario.Cliente = new Cliente { Usuario = usuario, Telefono = request.Telefono?.Trim(), Direccion = request.Direccion?.Trim() };
        db.Usuarios.Add(usuario);
        try
        {
            // EF inserts the principal and its shared-key profile in this transaction.
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 })
        {
            throw new ApiException("Ya existe una cuenta con esos datos únicos.", StatusCodes.Status409Conflict);
        }
        return usuario;
    }

    public async Task<bool> ValidarTokenAsync(ClaimsPrincipal principal, CancellationToken cancellationToken = default)
    {
        if (!int.TryParse(principal.FindFirst("sub")?.Value, NumberStyles.None, CultureInfo.InvariantCulture, out var id) || id <= 0)
            return false;
        var roles = principal.FindAll("role").ToList();
        if (roles.Count != 1) return false;
        var usuario = await Cuentas.SingleOrDefaultAsync(u => u.Id == id, cancellationToken);
        return usuario is not null && PerfilValido(usuario) && usuario.Rol == roles[0].Value &&
            principal.FindFirst("auth_version")?.Value == CredentialVersion(usuario);
    }

    private static bool PerfilValido(Usuario usuario) => !usuario.Eliminado && (usuario.Rol switch
    {
        Usuario.RolAdministrador => usuario.Empleado is null && usuario.Cliente is null,
        Usuario.RolEmpleado => usuario.Empleado is not null && usuario.Cliente is null,
        Usuario.RolCliente => usuario.Cliente is not null && usuario.Empleado is null,
        _ => false
    });

    private static string CredentialVersion(Usuario usuario) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(usuario.PasswordHash)));

    private static UsuarioDto ToDto(Usuario usuario) =>
        new(usuario.Id, usuario.Nombre, usuario.Apellido, usuario.CUIT, usuario.Email, usuario.Rol, usuario.Eliminado);

    public static void ValidarPassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length is < 12 or > 128)
            throw new ApiException("La contraseña debe tener entre 12 y 128 caracteres.");
    }

    private static void ValidateRegistration(RegisterRequest request)
    {
        // Also validate service callers such as the bootstrap CLI, outside MVC validation.
        var errors = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        if (!System.ComponentModel.DataAnnotations.Validator.TryValidateObject(request,
            new System.ComponentModel.DataAnnotations.ValidationContext(request), errors, true) ||
            string.IsNullOrWhiteSpace(request.Nombre) || string.IsNullOrWhiteSpace(request.Apellido) ||
            string.IsNullOrWhiteSpace(request.CUIT) || string.IsNullOrWhiteSpace(request.Password))
            throw new ApiException("Datos de registro inválidos: revise nombre, apellido, CUIT, email y contraseña (12 a 128 caracteres).");
        ValidarPassword(request.Password);
        if (request.Rol is not (Usuario.RolAdministrador or Usuario.RolEmpleado or Usuario.RolCliente))
            throw new ApiException("Rol inválido. Use Administrador, Empleado o Cliente.");
        if (request.Rol == Usuario.RolEmpleado)
        {
            if (request.FechaIngreso is null || request.FechaIngreso == DateOnly.MinValue ||
                request.Telefono is not null || request.Direccion is not null)
                throw new ApiException("Un empleado requiere FechaIngreso y no admite datos de perfil Cliente.");
        }
        else if (request.Rol == Usuario.RolCliente)
        {
            if (request.FechaIngreso is not null)
                throw new ApiException("Un cliente no admite datos de perfil Empleado.");
        }
        else if (request.FechaIngreso is not null || request.Telefono is not null || request.Direccion is not null)
            throw new ApiException("Un administrador no tiene subperfil.");
    }
}
