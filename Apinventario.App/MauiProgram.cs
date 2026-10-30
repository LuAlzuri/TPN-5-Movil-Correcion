using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Components.Authorization;
using Apinventario.App.Services.Infrastructure;
using Apinventario.App.Services.Administracion;

namespace Apinventario.App;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
        System.Globalization.CultureInfo.DefaultThreadCurrentCulture = new("es-AR");
        System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = new("es-AR");
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
			});

		builder.Services.AddMauiBlazorWebView();
		builder.Services.AddAuthorizationCore();
		builder.Services.AddCascadingAuthenticationState();
		builder.Services.AddSingleton(_ => {
            var config = new ApiConfig(Preferences.Default.Get("api-base", "http://10.0.2.2:5087/"));
            config.Changed += uri => Preferences.Default.Set("api-base", uri.ToString());
            return config;
        });
		builder.Services.AddSingleton<ITokenStore, TokenStore>();
		builder.Services.AddSingleton(new HttpClient { Timeout = TimeSpan.FromSeconds(30) });
		builder.Services.AddSingleton(_ => new AppErrorLogger(Path.Combine(FileSystem.AppDataDirectory, "diagnostics")));
		builder.Services.AddSingleton<ApiClient>();
		builder.Services.AddSingleton<SessionService>();
		builder.Services.AddScoped<AuthenticationStateProvider, SessionAuthenticationStateProvider>();
		builder.Services.AddSingleton<ConfirmService>();
		builder.Services.AddSingleton<FileService>();
		builder.Services.AddSingleton<AdminClient>();
		builder.Services.AddSingleton<AuditClient>();
		builder.Services.AddSingleton<ReportsClient>();

#if DEBUG
		builder.Services.AddBlazorWebViewDeveloperTools();
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
