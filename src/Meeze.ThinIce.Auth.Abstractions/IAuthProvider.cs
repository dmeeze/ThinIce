using Meeze.ThinIce.Auth.Models;

namespace Meeze.ThinIce.Auth;

public interface IAuthProvider
{
    string ProviderKey { get; }
    Task<TenantContext?> ValidateTokenAsync(string token, CancellationToken ct = default);
    Task<OAuthTokenResponse?> ExchangeTokenAsync(OAuthTokenRequest request, CancellationToken ct = default);
}
