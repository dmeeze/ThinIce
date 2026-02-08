using Meeze.ThinIce.Iceberg.Models;

namespace Meeze.ThinIce.App.Endpoints;

public static class ConfigEndpoints
{
    public static RouteGroupBuilder MapConfigEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/v1").RequireRateLimiting("config");

        group.MapGet("/config", () =>
        {
            var config = new CatalogConfig(
                Defaults: new Dictionary<string, string>
                {
                    ["prefix"] = "v1"
                },
                Overrides: new Dictionary<string, string>());
            return Results.Json(config, AppJsonSerializerContext.Default.CatalogConfig);
        });

        return group;
    }
}
