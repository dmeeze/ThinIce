using Meeze.ThinIce.Iceberg.Models;
using Microsoft.Extensions.Options;

namespace Meeze.ThinIce.App.Endpoints;

public static class ConfigEndpoints
{
    public static RouteGroupBuilder MapConfigEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/v1").RequireRateLimiting("config");

        group.MapGet("/config", (IOptions<ConfigOptions> configOptions) =>
        {
            var opts = configOptions.Value;
            var config = new CatalogConfig(
                Defaults: new Dictionary<string, string>
                {
                    ["prefix"] = opts.Prefix
                },
                Overrides: new Dictionary<string, string>
                {
                    ["oauth2-server-uri"] = opts.OAuth2ServerUri
                });
            return Results.Json(config, AppJsonSerializerContext.Default.CatalogConfig);
        });

        return group;
    }
}
