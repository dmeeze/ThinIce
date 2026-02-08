using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Meeze.ThinIce.Auth;
using Meeze.ThinIce.Auth.Models;

namespace Meeze.ThinIce.Iceberg.LocalDev;

public sealed partial class LocalDevAuthProvider : IAuthProvider
{
    public const string Key = "LocalDev";

    private readonly Dictionary<string, (string Tenant, string User)> _tokens;
    private readonly ILogger<LocalDevAuthProvider> _logger;

    public string ProviderKey => Key;

    public LocalDevAuthProvider(IOptions<LocalDevOptions> options, ILogger<LocalDevAuthProvider> logger)
    {
        _logger = logger;
        _tokens = ParseTokens(options.Value.Tokens);
    }

    public static Dictionary<string, (string Tenant, string User)> ParseTokens(List<string> tokenStrings)
    {
        var result = new Dictionary<string, (string Tenant, string User)>(StringComparer.Ordinal);

        foreach (var entry in tokenStrings)
        {
            var parts = entry.Split(':', 3);
            if (parts.Length != 3)
                continue;

            result[parts[0]] = (parts[1], parts[2]);
        }

        return result;
    }

    public Task<TenantContext?> ValidateTokenAsync(string token, CancellationToken ct = default)
    {
        if (_tokens.TryGetValue(token, out var identity))
        {
            LogTokenValidated(identity.Tenant, identity.User);
            return Task.FromResult<TenantContext?>(new TenantContext(identity.Tenant, identity.User, Key));
        }

        return Task.FromResult<TenantContext?>(null);
    }

    public Task<OAuthTokenResponse?> ExchangeTokenAsync(OAuthTokenRequest request, CancellationToken ct = default)
    {
        if (request.GrantType != "client_credentials")
            return Task.FromResult<OAuthTokenResponse?>(null);

        // For LocalDev, the client_id is the static token itself
        if (_tokens.TryGetValue(request.ClientId, out var identity))
        {
            LogTokenExchanged(identity.Tenant, identity.User);
            return Task.FromResult<OAuthTokenResponse?>(
                new OAuthTokenResponse(AccessToken: request.ClientId, Scope: request.Scope));
        }

        return Task.FromResult<OAuthTokenResponse?>(null);
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "LocalDev token validated for {Tenant}/{User}")]
    private partial void LogTokenValidated(string tenant, string user);

    [LoggerMessage(Level = LogLevel.Debug, Message = "LocalDev token exchanged for {Tenant}/{User}")]
    private partial void LogTokenExchanged(string tenant, string user);
}
