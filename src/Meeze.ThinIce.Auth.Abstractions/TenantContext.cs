namespace Meeze.ThinIce.Auth;

public record TenantContext(string Tenant, string User, string ProviderKey)
{
    public bool IsAuthenticated => !string.IsNullOrEmpty(Tenant);

    public static TenantContext Anonymous => new("", "", "");
}
