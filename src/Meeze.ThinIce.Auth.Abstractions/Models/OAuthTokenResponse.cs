using System.Text.Json.Serialization;

namespace Meeze.ThinIce.Auth.Models;

public record OAuthTokenResponse(
    [property: JsonPropertyName("access_token")] string AccessToken,
    [property: JsonPropertyName("token_type")] string TokenType = "bearer",
    [property: JsonPropertyName("expires_in")] int ExpiresIn = 3600,
    [property: JsonPropertyName("scope")] string? Scope = null);
