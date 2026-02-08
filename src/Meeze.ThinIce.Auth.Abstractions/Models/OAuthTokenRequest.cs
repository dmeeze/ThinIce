namespace Meeze.ThinIce.Auth.Models;

public record OAuthTokenRequest(string GrantType, string ClientId, string ClientSecret, string? Scope = null)
{
    public const string ClientCredentialsGrantType = "client_credentials";
}
