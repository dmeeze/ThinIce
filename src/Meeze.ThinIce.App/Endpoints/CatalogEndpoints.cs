using Meeze.ThinIce.Auth;
using Meeze.ThinIce.Iceberg;
using Meeze.ThinIce.Iceberg.Models;

namespace Meeze.ThinIce.App.Endpoints;

public static class CatalogEndpoints
{
    public static RouteGroupBuilder MapCatalogEndpoints(this IEndpointRouteBuilder routes, string? prefix)
    {
        var group = routes.MapPrefixedGroup(prefix, "/tables").RequireRateLimiting("authenticated");

        group.MapPost("/rename", async (HttpContext context, IIcebergRouter router, RenameTableRequest request) =>
        {
            var tenant = context.Features.Get<TenantContext>()!;
            var catalog = router.GetProvider(tenant.Tenant).GetCatalog(tenant.Tenant);
            try
            {
                await catalog.RenameTableAsync(request.Source, request.Destination, context.RequestAborted);
                return Results.NoContent();
            }
            catch (DirectoryNotFoundException ex)
            {
                return Results.Json(
                    new IcebergErrorResponse(new IcebergError(
                        ex.Message,
                        "NoSuchNamespaceException", 404)),
                    AppJsonSerializerContext.Default.IcebergErrorResponse,
                    statusCode: 404);
            }
            catch (FileNotFoundException ex)
            {
                return Results.Json(
                    new IcebergErrorResponse(new IcebergError(
                        ex.Message,
                        "NoSuchTableException", 404)),
                    AppJsonSerializerContext.Default.IcebergErrorResponse,
                    statusCode: 404);
            }
            catch (InvalidOperationException ex)
            {
                return Results.Json(
                    new IcebergErrorResponse(new IcebergError(
                        ex.Message,
                        "AlreadyExistsException", 409)),
                    AppJsonSerializerContext.Default.IcebergErrorResponse,
                    statusCode: 409);
            }
        });

        return group;
    }
}
