using Meeze.ThinIce;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Meeze.ThinIce.Iceberg.LocalDev;

public static class ServiceExtensions
{
    public static IServiceCollection AddLocalDevProvider(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<Options>(options =>
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

        services.AddSingleton<AuthProvider>();
        services.AddSingleton<IAuthProvider>(sp => sp.GetRequiredService<AuthProvider>());

        // Register provider for multi-provider routing
        services.AddSingleton<IIcebergProvider, Provider>();

        return services;
    }
}
