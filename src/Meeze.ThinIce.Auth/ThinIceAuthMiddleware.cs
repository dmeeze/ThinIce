using System.Net;
using System.Text.Json;
using Meeze.ThinIce.Iceberg.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Meeze.ThinIce.Auth;

public sealed partial class ThinIceAuthMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IEnumerable<IAuthProvider> _providers;
    private readonly ThinIceAuthOptions _options;
    private readonly ILogger<ThinIceAuthMiddleware> _logger;

    public ThinIceAuthMiddleware(
        RequestDelegate next,
        IEnumerable<IAuthProvider> providers,
        IOptions<ThinIceAuthOptions> options,
        ILogger<ThinIceAuthMiddleware> logger)
    {
        _next = next;
        _providers = providers;
        _options = options.Value;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (IsAnonymousEndpoint(context))
        {
            await _next(context);
            return;
        }

        var authHeader = context.Request.Headers.Authorization.ToString();
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            await WriteUnauthorized(context, "Missing or invalid Authorization header");
            return;
        }

        var token = authHeader["Bearer ".Length..].Trim();

        foreach (var provider in _providers)
        {
            var tenant = await provider.ValidateTokenAsync(token, context.RequestAborted);
            if (tenant is not null)
            {
                LogAuthenticated(tenant.Tenant, tenant.User, provider.ProviderKey);
                context.Features.Set(tenant);
                await _next(context);
                return;
            }
        }

        LogAuthFailed();
        await WriteUnauthorized(context, "Invalid or expired token");
    }

    private bool IsAnonymousEndpoint(HttpContext context)
    {
        var path = context.Request.Path.Value ?? "";
        var method = context.Request.Method;

        foreach (var ep in _options.AnonymousEndpoints)
        {
            if (!path.Equals(ep.Path, StringComparison.OrdinalIgnoreCase))
                continue;

            if (ep.Method is null || ep.Method.Equals(method, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private static async Task WriteUnauthorized(HttpContext context, string message)
    {
        context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
        context.Response.ContentType = "application/json";
        var error = new IcebergErrorResponse(new IcebergError(message, "NotAuthorizedException", 401));
        await JsonSerializer.SerializeAsync(context.Response.Body, error, AuthJsonContext.Default.IcebergErrorResponse);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Authenticated {Tenant}/{User} via {Provider}")]
    private partial void LogAuthenticated(string tenant, string user, string provider);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Authentication failed: no provider accepted the token")]
    private partial void LogAuthFailed();
}
