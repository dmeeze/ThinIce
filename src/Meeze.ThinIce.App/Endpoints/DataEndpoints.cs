using Meeze.ThinIce.Auth;
using Meeze.ThinIce.Iceberg;
using Meeze.ThinIce.Iceberg.Models;

namespace Meeze.ThinIce.App.Endpoints;

public static class DataEndpoints
{
    public static RouteGroupBuilder MapDataEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/v1/data");

        group.MapGet("/{**path}", async (HttpContext context, IIcebergStorageResolver resolver, string path) =>
        {
            var tenant = context.Features.Get<TenantContext>()!;
            var storage = resolver.GetStorage(tenant.Tenant);
            try
            {
                var stream = await storage.ReadFileAsync(path, context.RequestAborted);
                return Results.Stream(stream, contentType: "application/octet-stream");
            }
            catch (FileNotFoundException)
            {
                return Results.Json(
                    new IcebergErrorResponse(new IcebergError(
                        $"Data file lost in the blizzard — {path} not found",
                        "NotFoundException", 404)),
                    AppJsonSerializerContext.Default.IcebergErrorResponse,
                    statusCode: 404);
            }
        });

        group.MapPut("/{**path}", async (HttpContext context, IIcebergStorageResolver resolver, string path) =>
        {
            var tenant = context.Features.Get<TenantContext>()!;
            var storage = resolver.GetStorage(tenant.Tenant);
            await storage.WriteFileAsync(path, context.Request.Body, context.RequestAborted);
            return Results.NoContent();
        });

        return group;
    }
}
