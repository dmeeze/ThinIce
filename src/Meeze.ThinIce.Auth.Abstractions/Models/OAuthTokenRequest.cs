namespace Meeze.ThinIce.Auth.Models;

public record OAuthTokenRequest(string GrantType, string ClientId, string ClientSecret, string? Scope = null);
