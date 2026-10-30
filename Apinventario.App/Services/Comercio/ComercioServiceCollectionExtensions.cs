using Microsoft.Extensions.DependencyInjection;

namespace Apinventario.App.Services.Comercio;

public static class ComercioServiceCollectionExtensions
{
    public static IServiceCollection AddComercio(this IServiceCollection services)
    {
        services.AddSingleton<TiendaState>();
        services.AddSingleton<CarritoState>();
        services.AddSingleton<CatalogoClient>();
        services.AddSingleton<ProveedoresClient>();
        services.AddSingleton<CarritoClient>();
        services.AddSingleton<IngresosClient>();
        services.AddSingleton<ClientesClient>();
        return services;
    }
}
