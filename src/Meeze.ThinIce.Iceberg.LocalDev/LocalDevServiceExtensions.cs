using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Meeze.ThinIce.Auth;

namespace Meeze.ThinIce.Iceberg.LocalDev;

public static class LocalDevServiceExtensions
{
    public static IServiceCollection AddLocalDevProvider(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<LocalDevOptions>(options =>
        {
            var section = configuration.GetSection("LocalDev");
            options.BasePath = section["BasePath"];

            var tokensSection = section.GetSection("Tokens");
            if (tokensSection.Exists())
            {
                foreach (var child in tokensSection.GetChildren())
                {
                    if (child.Value is not null)
                    {
                        options.Tokens.Add(child.Value);
                    }
                }
            }
        });

        services.AddSingleton<LocalDevAuthProvider>();
        services.AddSingleton<IAuthProvider>(sp => sp.GetRequiredService<LocalDevAuthProvider>());

        services.AddSingleton<LocalDevCatalogResolver>();
        services.AddSingleton<IIcebergCatalogResolver>(sp => sp.GetRequiredService<LocalDevCatalogResolver>());

        services.AddSingleton<LocalDevStorageResolver>();
        services.AddSingleton<IIcebergStorageResolver>(sp => sp.GetRequiredService<LocalDevStorageResolver>());

        return services;
    }
}
