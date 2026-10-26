using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace TPN_5_MOVIL.Security;

public sealed record JwtSettings(string Issuer, string Audience, string Key, int ExpireMinutes)
{
    public static JwtSettings Load(IConfiguration configuration)
    {
        var issuer = configuration["Jwt:Issuer"];
        var audience = configuration["Jwt:Audience"];
        var key = configuration["Jwt:Key"];
        var lifetime = configuration["Jwt:ExpireMinutes"] ?? "120";
        if (string.IsNullOrWhiteSpace(issuer) || string.IsNullOrWhiteSpace(audience) ||
            string.IsNullOrWhiteSpace(key) || Encoding.UTF8.GetByteCount(key) < 32 ||
            !int.TryParse(lifetime, out var minutes) || minutes is < 1 or > 1440)
            throw new InvalidOperationException("Configure Jwt:Issuer, Jwt:Audience, Jwt:Key (al menos 32 bytes) y Jwt:ExpireMinutes (1 a 1440). No se admiten credenciales predeterminadas.");
        return new JwtSettings(issuer, audience, key, minutes);
    }

    public SymmetricSecurityKey SigningKey => new(Encoding.UTF8.GetBytes(Key));
}
