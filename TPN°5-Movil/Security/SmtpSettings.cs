namespace TPN_5_MOVIL.Security;

public sealed class SmtpSettings
{
    public string Host { get; set; } = string.Empty;
    public int Puerto { get; set; } = 587;
    public bool UsarSsl { get; set; } = true;
    public string Remitente { get; set; } = string.Empty;
    public string Usuario { get; set; } = string.Empty;
    public string Contrasena { get; set; } = string.Empty;
    public bool Habilitado { get; set; } = false;
    public int TimeoutSegundos { get; set; } = 30;

    public static SmtpSettings Load(IConfiguration config)
    {
        var settings = new SmtpSettings();
        config.GetSection("Smtp").Bind(settings);
        if (settings.Puerto <= 0) settings.Puerto = 587;
        if (settings.TimeoutSegundos <= 0) settings.TimeoutSegundos = 30;
        if (settings.TimeoutSegundos > 300) settings.TimeoutSegundos = 300;
        return settings;
    }

    public bool EstaConfigurado()
    {
        if (!Habilitado) return false;
        if (string.IsNullOrWhiteSpace(Host)) return false;
        if (string.IsNullOrWhiteSpace(Remitente)) return false;
        return true;
    }
}
