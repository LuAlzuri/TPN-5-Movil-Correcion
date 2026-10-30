namespace Apinventario.App.Services.Infrastructure;

public sealed class ApiConfig
{
    public Uri BaseUri { get; private set; }
    public event Action<Uri>? Changed;
    public ApiConfig(string? configuredAddress = null)
    {
#if DEBUG
        BaseUri = Validate(configuredAddress ?? "http://10.0.2.2:5087/", true);
#else
        // Set a trusted HTTPS deployment before distributing Release builds.
        BaseUri = new Uri("https://api.apinventario.invalid/");
#endif
    }
    public static Uri Validate(string value, bool allowLocalHttp)
    {
        if (!Uri.TryCreate(value.Trim().TrimEnd('/') + "/", UriKind.Absolute, out var uri)
            || !string.IsNullOrEmpty(uri.UserInfo) || !string.IsNullOrEmpty(uri.Query)
            || !string.IsNullOrEmpty(uri.Fragment) || uri.AbsolutePath != "/")
            throw new ArgumentException("Indicá el origen de la API sin credenciales, ruta, query ni fragmento.");
        var local = uri.IsLoopback || uri.Host == "10.0.2.2" ||
            (System.Net.IPAddress.TryParse(uri.Host, out var ip) && ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork &&
             (ip.GetAddressBytes()[0] == 10 || (ip.GetAddressBytes()[0] == 192 && ip.GetAddressBytes()[1] == 168) ||
              (ip.GetAddressBytes()[0] == 172 && ip.GetAddressBytes()[1] is >= 16 and <= 31)));
        if (uri.Scheme != "https" && !(allowLocalHttp && local && uri.Scheme == "http"))
            throw new ArgumentException("Usá HTTPS; HTTP solo se admite en desarrollo sobre red local.");
        return uri;
    }
    public void SetDevelopmentAddress(string value)
    {
#if DEBUG
        BaseUri = Validate(value, true);
        Changed?.Invoke(BaseUri);
#else
        throw new InvalidOperationException("El servidor de Release se configura al compilar.");
#endif
    }
    public string? ResolveImageUrl(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        if (!Uri.TryCreate(BaseUri, value, out var uri) || uri.Scheme is not ("https" or "http")) return null;
        return uri.ToString();
    }
}
