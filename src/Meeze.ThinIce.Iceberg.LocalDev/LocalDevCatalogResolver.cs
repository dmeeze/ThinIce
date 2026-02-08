using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Meeze.ThinIce.Iceberg.LocalDev;

public sealed partial class LocalDevCatalogResolver : IIcebergCatalogResolver, IDisposable
{
    private readonly LocalDevOptions _options;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ConcurrentDictionary<string, LocalDevCatalog> _tenants = new(StringComparer.Ordinal);

    public LocalDevCatalogResolver(IOptions<LocalDevOptions> options, ILoggerFactory loggerFactory)
    {
        _options = options.Value;
        _loggerFactory = loggerFactory;
    }

    public IIcebergCatalog GetCatalog(string tenant) =>
        _tenants.GetOrAdd(tenant, t => new LocalDevCatalog(
            Path.Combine(_options.ResolvedBasePath, t, "namespaces"),
            _loggerFactory.CreateLogger<LocalDevCatalog>()));

    public void Dispose()
    {
        foreach (var catalog in _tenants.Values)
            catalog.Dispose();
    }
}
