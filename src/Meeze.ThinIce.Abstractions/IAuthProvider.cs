namespace Meeze.ThinIce;

public interface IAuthProvider
{
    string ProviderKey { get; }
    Task<TenantContext?> ValidateTokenAsync(string token, CancellationToken ct = default);
}
