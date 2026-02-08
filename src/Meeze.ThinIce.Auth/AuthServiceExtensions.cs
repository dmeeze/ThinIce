using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Meeze.ThinIce.Auth;

public static class AuthServiceExtensions
{
    public static IServiceCollection AddThinIceAuth(this IServiceCollection services, Action<ThinIceAuthOptions>? configure = null)
    {
        services.AddScoped(_ => TenantContext.Anonymous);

        var options = new ThinIceAuthOptions();
        configure?.Invoke(options);
        services.AddSingleton(Microsoft.Extensions.Options.Options.Create(options));

        return services;
    }

    public static IApplicationBuilder UseThinIceAuth(this IApplicationBuilder app)
    {
        app.UseMiddleware<ThinIceAuthMiddleware>();
        return app;
    }
}
