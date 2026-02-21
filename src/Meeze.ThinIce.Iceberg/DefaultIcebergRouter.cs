using Microsoft.Extensions.Logging;


namespace Meeze.ThinIce.Iceberg;

public sealed partial class DefaultIcebergRouter(
    IEnumerable<IIcebergProvider> providers,
    ILogger<DefaultIcebergRouter> logger) : IIcebergRouter
{

    public IIcebergProvider GetProvider(string tenant)
    {
        
        foreach (var provider in providers)
        {
            if (provider.CanHandle(tenant))
            {
                LogProviderSelected(tenant, provider.GetType().Name);
                return provider;
            }
        }

        LogNoProviderFound(tenant);
        throw new InvalidOperationException($"No provider found for tenant: {tenant}");
    }


    [LoggerMessage(Level = LogLevel.Debug, Message = "Resolver cache hit for tenant: {Tenant}")]
    private partial void LogResolverCacheHit(string tenant);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Provider {ProviderType} selected for tenant: {Tenant}")]
    private partial void LogProviderSelected(string tenant, string providerType);

    [LoggerMessage(Level = LogLevel.Error, Message = "No provider found for tenant: {Tenant}")]
    private partial void LogNoProviderFound(string tenant);
}
