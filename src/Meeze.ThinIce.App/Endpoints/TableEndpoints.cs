using Meeze.ThinIce.Auth;
using Meeze.ThinIce.Iceberg;
using Meeze.ThinIce.Iceberg.Models;

namespace Meeze.ThinIce.App.Endpoints;

public static class TableEndpoints
{
    public static RouteGroupBuilder MapTableEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/v1/namespaces/{ns}/tables");

        group.MapGet("/", async (HttpContext context, IIcebergCatalogResolver resolver, string ns) =>
        {
            var tenant = context.Features.Get<TenantContext>()!;
            var catalog = resolver.GetCatalog(tenant.Tenant);
            var levels = NamespaceHelpers.Parse(ns);
            try
            {
                var result = await catalog.ListTablesAsync(levels, context.RequestAborted);
                return Results.Json(result, AppJsonSerializerContext.Default.ListTablesResponse);
            }
            catch (DirectoryNotFoundException)
            {
                return Results.Json(
                    new IcebergErrorResponse(new IcebergError(
                        $"Namespace vanished into thin ice — {ns} not found",
                        "NoSuchNamespaceException", 404)),
                    AppJsonSerializerContext.Default.IcebergErrorResponse,
                    statusCode: 404);
            }
        });

        group.MapPost("/", async (HttpContext context, IIcebergCatalogResolver resolver, string ns, CreateTableRequest request) =>
        {
            var tenant = context.Features.Get<TenantContext>()!;
            var catalog = resolver.GetCatalog(tenant.Tenant);
            var levels = NamespaceHelpers.Parse(ns);
            try
            {
                var result = await catalog.CreateTableAsync(levels, request, context.RequestAborted);
                return Results.Json(result, AppJsonSerializerContext.Default.LoadTableResponse);
            }
            catch (DirectoryNotFoundException)
            {
                return Results.Json(
                    new IcebergErrorResponse(new IcebergError(
                        $"Namespace vanished into thin ice — {ns} not found",
                        "NoSuchNamespaceException", 404)),
                    AppJsonSerializerContext.Default.IcebergErrorResponse,
                    statusCode: 404);
            }
            catch (InvalidOperationException)
            {
                return Results.Json(
                    new IcebergErrorResponse(new IcebergError(
                        $"Table already frozen solid — it already exists in {ns}",
                        "AlreadyExistsException", 409)),
                    AppJsonSerializerContext.Default.IcebergErrorResponse,
                    statusCode: 409);
            }
        });

        group.MapGet("/{table}", async (HttpContext context, IIcebergCatalogResolver resolver, string ns, string table) =>
        {
            var tenant = context.Features.Get<TenantContext>()!;
            var catalog = resolver.GetCatalog(tenant.Tenant);
            var levels = NamespaceHelpers.Parse(ns);
            try
            {
                var result = await catalog.LoadTableAsync(levels, table, context.RequestAborted);
                return Results.Json(result, AppJsonSerializerContext.Default.LoadTableResponse);
            }
            catch (DirectoryNotFoundException)
            {
                return Results.Json(
                    new IcebergErrorResponse(new IcebergError(
                        $"Namespace vanished into thin ice — {ns} not found",
                        "NoSuchNamespaceException", 404)),
                    AppJsonSerializerContext.Default.IcebergErrorResponse,
                    statusCode: 404);
            }
            catch (FileNotFoundException)
            {
                return Results.Json(
                    new IcebergErrorResponse(new IcebergError(
                        $"Table slipped through the ice — {table} not found in {ns}",
                        "NoSuchTableException", 404)),
                    AppJsonSerializerContext.Default.IcebergErrorResponse,
                    statusCode: 404);
            }
        });

        group.MapDelete("/{table}", async (HttpContext context, IIcebergCatalogResolver resolver, string ns, string table) =>
        {
            var tenant = context.Features.Get<TenantContext>()!;
            var catalog = resolver.GetCatalog(tenant.Tenant);
            var levels = NamespaceHelpers.Parse(ns);
            try
            {
                await catalog.DropTableAsync(levels, table, context.RequestAborted);
                return Results.NoContent();
            }
            catch (DirectoryNotFoundException)
            {
                return Results.Json(
                    new IcebergErrorResponse(new IcebergError(
                        $"Namespace vanished into thin ice — {ns} not found",
                        "NoSuchNamespaceException", 404)),
                    AppJsonSerializerContext.Default.IcebergErrorResponse,
                    statusCode: 404);
            }
            catch (FileNotFoundException)
            {
                return Results.Json(
                    new IcebergErrorResponse(new IcebergError(
                        $"Table slipped through the ice — {table} not found in {ns}",
                        "NoSuchTableException", 404)),
                    AppJsonSerializerContext.Default.IcebergErrorResponse,
                    statusCode: 404);
            }
        });

        return group;
    }
}
