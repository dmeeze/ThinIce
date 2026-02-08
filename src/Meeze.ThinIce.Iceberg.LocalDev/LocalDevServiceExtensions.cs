using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Meeze.ThinIce.Auth;

namespace Meeze.ThinIce.Iceberg.LocalDev;

public static class LocalDevServiceExtensions
{
    public static IServiceCollection AddLocalDevProvider(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<LocalDevOptions>(configuration.GetSection("LocalDev"));

        services.AddSingleton<LocalDevAuthProvider>();
        services.AddSingleton<IAuthProvider>(sp => sp.GetRequiredService<LocalDevAuthProvider>());

        services.AddSingleton<LocalDevCatalogResolver>();
        services.AddSingleton<IIcebergCatalogResolver>(sp => sp.GetRequiredService<LocalDevCatalogResolver>());

        return services;
    }
}
