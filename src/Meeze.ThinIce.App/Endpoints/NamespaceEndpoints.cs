using Meeze.ThinIce.Auth;
using Meeze.ThinIce.Iceberg;
using Meeze.ThinIce.Iceberg.Models;

namespace Meeze.ThinIce.App.Endpoints;

public static class NamespaceEndpoints
{
    public static RouteGroupBuilder MapNamespaceEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/v1/namespaces").RequireRateLimiting("authenticated");

        group.MapGet("/", async (HttpContext context, IIcebergCatalogResolver resolver) =>
        {
            var tenant = context.Features.Get<TenantContext>()!;
            var catalog = resolver.GetCatalog(tenant.Tenant);
            var result = await catalog.ListNamespacesAsync(context.RequestAborted);
            return Results.Json(result, AppJsonSerializerContext.Default.ListNamespacesResponse);
        });

        group.MapPost("/", async (HttpContext context, IIcebergCatalogResolver resolver, CreateNamespaceRequest request) =>
        {
            var tenant = context.Features.Get<TenantContext>()!;
            var catalog = resolver.GetCatalog(tenant.Tenant);
            try
            {
                var result = await catalog.CreateNamespaceAsync(request, context.RequestAborted);
                return Results.Json(result, AppJsonSerializerContext.Default.NamespaceDetail);
            }
            catch (InvalidOperationException)
            {
                return Results.Json(
                    new IcebergErrorResponse(new IcebergError(
                        $"Namespace already exists: {string.Join(".", request.Namespace)}",
                        "AlreadyExistsException", 409)),
                    AppJsonSerializerContext.Default.IcebergErrorResponse,
                    statusCode: 409);
            }
        });

        group.MapGet("/{ns}", async (HttpContext context, IIcebergCatalogResolver resolver, string ns) =>
        {
            var tenant = context.Features.Get<TenantContext>()!;
            var catalog = resolver.GetCatalog(tenant.Tenant);
            var levels = NamespaceHelpers.Parse(ns);
            try
            {
                var result = await catalog.LoadNamespaceAsync(levels, context.RequestAborted);
                return Results.Json(result, AppJsonSerializerContext.Default.NamespaceDetail);
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

        group.MapDelete("/{ns}", async (HttpContext context, IIcebergCatalogResolver resolver, string ns) =>
        {
            var tenant = context.Features.Get<TenantContext>()!;
            var catalog = resolver.GetCatalog(tenant.Tenant);
            var levels = NamespaceHelpers.Parse(ns);
            try
            {
                await catalog.DropNamespaceAsync(levels, context.RequestAborted);
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
        });

        return group;
    }
}
