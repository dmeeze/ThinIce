using Meeze.ThinIce;
using Meeze.ThinIce.Iceberg;
using Meeze.ThinIce.Models;

namespace Meeze.ThinIce.App.Endpoints;

public static class TableEndpoints
{
    public static RouteGroupBuilder MapTableEndpoints(this IEndpointRouteBuilder routes, string? prefix)
    {
        var group = routes.MapPrefixedGroup(prefix, "/namespaces/{ns}/tables").RequireRateLimiting("authenticated");

        group.MapGet("/", async (HttpContext context, IIcebergRouter router, string ns) =>
        {
            var tenant = context.Features.Get<TenantContext>()!;
            var catalog = router.GetProvider(tenant.Tenant).GetCatalog(tenant.Tenant);
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
                        $"Namespace not found: {ns}",
                        "NoSuchNamespaceException", 404)),
                    AppJsonSerializerContext.Default.IcebergErrorResponse,
                    statusCode: 404);
            }
        });

        group.MapPost("/", async (HttpContext context, IIcebergRouter router, string ns, CreateTableRequest request) =>
        {
            var tenant = context.Features.Get<TenantContext>()!;
            var catalog = router.GetProvider(tenant.Tenant).GetCatalog(tenant.Tenant);
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
                        $"Namespace not found: {ns}",
                        "NoSuchNamespaceException", 404)),
                    AppJsonSerializerContext.Default.IcebergErrorResponse,
                    statusCode: 404);
            }
            catch (InvalidOperationException)
            {
                return Results.Json(
                    new IcebergErrorResponse(new IcebergError(
                        $"Table already exists in namespace: {ns}",
                        "AlreadyExistsException", 409)),
                    AppJsonSerializerContext.Default.IcebergErrorResponse,
                    statusCode: 409);
            }
        });

        group.MapGet("/{table}", async (HttpContext context, IIcebergRouter router, string ns, string table) =>
        {
            var tenant = context.Features.Get<TenantContext>()!;
            var catalog = router.GetProvider(tenant.Tenant).GetCatalog(tenant.Tenant);
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
                        $"Namespace not found: {ns}",
                        "NoSuchNamespaceException", 404)),
                    AppJsonSerializerContext.Default.IcebergErrorResponse,
                    statusCode: 404);
            }
            catch (FileNotFoundException)
            {
                return Results.Json(
                    new IcebergErrorResponse(new IcebergError(
                        $"Table not found: {table} in namespace {ns}",
                        "NoSuchTableException", 404)),
                    AppJsonSerializerContext.Default.IcebergErrorResponse,
                    statusCode: 404);
            }
        });

        group.MapPost("/{table}", async (HttpContext context, IIcebergRouter router, string ns, string table, CommitTableRequest request) =>
        {
            var tenant = context.Features.Get<TenantContext>()!;
            var catalog = router.GetProvider(tenant.Tenant).GetCatalog(tenant.Tenant);
            var levels = NamespaceHelpers.Parse(ns);
            try
            {
                var result = await catalog.CommitTableAsync(levels, table, request, context.RequestAborted);
                return Results.Json(result, AppJsonSerializerContext.Default.LoadTableResponse);
            }
            catch (DirectoryNotFoundException)
            {
                return Results.Json(
                    new IcebergErrorResponse(new IcebergError(
                        $"Namespace not found: {ns}",
                        "NoSuchNamespaceException", 404)),
                    AppJsonSerializerContext.Default.IcebergErrorResponse,
                    statusCode: 404);
            }
            catch (FileNotFoundException)
            {
                return Results.Json(
                    new IcebergErrorResponse(new IcebergError(
                        $"Table not found: {table} in namespace {ns}",
                        "NoSuchTableException", 404)),
                    AppJsonSerializerContext.Default.IcebergErrorResponse,
                    statusCode: 404);
            }
            catch (InvalidOperationException ex)
            {
                return Results.Json(
                    new IcebergErrorResponse(new IcebergError(
                        ex.Message,
                        "CommitFailedException", 409)),
                    AppJsonSerializerContext.Default.IcebergErrorResponse,
                    statusCode: 409);
            }
        });

        group.MapMethods("/{table}", [HttpMethods.Head], async (HttpContext context, IIcebergRouter router, string ns, string table) =>
        {
            var tenant = context.Features.Get<TenantContext>()!;
            var catalog = router.GetProvider(tenant.Tenant).GetCatalog(tenant.Tenant);
            var levels = NamespaceHelpers.Parse(ns);
            var exists = await catalog.TableExistsAsync(levels, table, context.RequestAborted);
            return exists ? Results.NoContent() : Results.NotFound();
        });

        group.MapDelete("/{table}", async (HttpContext context, IIcebergRouter router, string ns, string table) =>
        {
            var tenant = context.Features.Get<TenantContext>()!;
            var catalog = router.GetProvider(tenant.Tenant).GetCatalog(tenant.Tenant);
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
                        $"Namespace not found: {ns}",
                        "NoSuchNamespaceException", 404)),
                    AppJsonSerializerContext.Default.IcebergErrorResponse,
                    statusCode: 404);
            }
            catch (FileNotFoundException)
            {
                return Results.Json(
                    new IcebergErrorResponse(new IcebergError(
                        $"Table not found: {table} in namespace {ns}",
                        "NoSuchTableException", 404)),
                    AppJsonSerializerContext.Default.IcebergErrorResponse,
                    statusCode: 404);
            }
        });

        return group;
    }
}
