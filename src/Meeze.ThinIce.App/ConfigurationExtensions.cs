namespace Meeze.ThinIce.App;

public static class ConfigurationExtensions
{
    public static IServiceCollection AddThinIceConfiguration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<ConfigOptions>(options =>
        {
            var section = configuration.GetSection("Config");
            var prefix = section["Prefix"];
            if (prefix is not null)
                options.Prefix = prefix;
            var oauth2ServerUri = section["OAuth2ServerUri"];
            if (oauth2ServerUri is not null)
                options.OAuth2ServerUri = oauth2ServerUri;
        });

        services.Configure<ThrottlingOptions>(options =>
        {
            var section = configuration.GetSection("Throttling");
            if (int.TryParse(section["ConfigPermitsPerMinute"], out var configPermits))
                options.ConfigPermitsPerMinute = configPermits;
            if (int.TryParse(section["AuthenticatedPermitsPerMinute"], out var authPermits))
                options.AuthenticatedPermitsPerMinute = authPermits;
        });

        return services;
    }
}
