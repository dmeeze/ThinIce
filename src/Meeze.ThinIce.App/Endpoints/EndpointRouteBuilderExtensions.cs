namespace Meeze.ThinIce.App.Endpoints;

public static class EndpointRouteBuilderExtensions
{
    public static RouteGroupBuilder MapPrefixedGroup(this IEndpointRouteBuilder routes, string? prefix, string path)
    {
        return routes.MapGroup(BuildPrefixedPath(prefix, path));
    }

    public static string BuildPrefixedPath(string? prefix, string path)
    {
        var trimmed = prefix?.Trim('/');
        return string.IsNullOrEmpty(trimmed) ? $"/v1{path}" : $"/v1/{trimmed}{path}";
    }
}
