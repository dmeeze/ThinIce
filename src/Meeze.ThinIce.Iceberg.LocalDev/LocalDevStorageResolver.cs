using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Meeze.ThinIce.Iceberg.LocalDev;

public sealed class LocalDevStorageResolver : IIcebergStorageResolver
{
    private readonly LocalDevOptions _options;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ConcurrentDictionary<string, LocalDevStorage> _tenants = new(StringComparer.Ordinal);

    public LocalDevStorageResolver(IOptions<LocalDevOptions> options, ILoggerFactory loggerFactory)
    {
        _options = options.Value;
        _loggerFactory = loggerFactory;
    }

    public IIcebergStorage GetStorage(string tenant) =>
        _tenants.GetOrAdd(tenant, t => new LocalDevStorage(
            Path.Combine(_options.ResolvedBasePath, t, "data"),
            _loggerFactory.CreateLogger<LocalDevStorage>()));
}
