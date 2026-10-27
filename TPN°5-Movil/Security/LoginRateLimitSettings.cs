namespace TPN_5_MOVIL.Security;

public sealed record LoginRateLimitSettings(int PermitLimit, int WindowSeconds)
{
    public const string PolicyName = "login";
    // System.Threading.Timer supports intervals up to uint.MaxValue - 1 milliseconds.
    public const int MaxWindowSeconds = 4_294_967;

    public static LoginRateLimitSettings Load(IConfiguration configuration)
    {
        var limit = configuration["LoginRateLimit:PermitLimit"] ?? "5";
        var window = configuration["LoginRateLimit:WindowSeconds"] ?? "60";
        if (!int.TryParse(limit, out var permits) || permits < 1 ||
            !int.TryParse(window, out var seconds) || seconds is < 1 or > MaxWindowSeconds)
            throw new InvalidOperationException("LoginRateLimit:PermitLimit y LoginRateLimit:WindowSeconds deben ser enteros positivos; WindowSeconds admite hasta 4294967 segundos.");
        return new LoginRateLimitSettings(permits, seconds);
    }
}
