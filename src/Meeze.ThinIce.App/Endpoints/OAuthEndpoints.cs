using Meeze.ThinIce.Auth;
using Meeze.ThinIce.Auth.Models;
using Meeze.ThinIce.Iceberg.Models;

namespace Meeze.ThinIce.App.Endpoints;

public static class OAuthEndpoints
{
    public static RouteGroupBuilder MapOAuthEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/v1/oauth");

        group.MapPost("/tokens", async (HttpContext context, IEnumerable<IAuthProvider> providers) =>
        {
            var form = await context.Request.ReadFormAsync(context.RequestAborted);

            var request = new OAuthTokenRequest(
                GrantType: form["grant_type"].ToString(),
                ClientId: form["client_id"].ToString(),
                ClientSecret: form["client_secret"].ToString(),
                Scope: form["scope"].ToString() is { Length: > 0 } scope ? scope : null);

            if (string.IsNullOrEmpty(request.GrantType) || string.IsNullOrEmpty(request.ClientId))
            {
                return Results.Json(
                    new IcebergErrorResponse(new IcebergError("grant_type and client_id are required", "BadRequestException", 400)),
                    AppJsonSerializerContext.Default.IcebergErrorResponse,
                    statusCode: 400);
            }

            foreach (var provider in providers)
            {
                var response = await provider.ExchangeTokenAsync(request, context.RequestAborted);
                if (response is not null)
                {
                    return Results.Json(response, AppJsonSerializerContext.Default.OAuthTokenResponse);
                }
            }

            return Results.Json(
                new IcebergErrorResponse(new IcebergError("Invalid credentials", "NotAuthorizedException", 401)),
                AppJsonSerializerContext.Default.IcebergErrorResponse,
                statusCode: 401);
        });

        return group;
    }
}
