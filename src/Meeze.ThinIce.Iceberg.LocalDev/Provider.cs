using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Meeze.ThinIce.Iceberg.LocalDev;

public sealed class Provider : IIcebergProvider
{
    private readonly IOptions<Options> _options;
    private readonly ILoggerFactory _loggerFactory;

    public Provider(IOptions<Options> options, ILoggerFactory loggerFactory)
    {
        _options = options;
        _loggerFactory = loggerFactory;
    }

    public bool CanHandle(string tenant) => true;
    
    private readonly ConcurrentDictionary<string, Catalog> _catalogs = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, Storage> _storages = new(StringComparer.Ordinal);

    public ICatalog GetCatalog(string tenant) =>
        _catalogs.GetOrAdd(
            tenant, 
            CreateCatalog
            );

    private Catalog CreateCatalog(string tenant) =>
        new Catalog(
            Path.Combine(_options.Value.ResolvedBasePath, tenant, "namespaces"),
            _loggerFactory.CreateLogger<Catalog>());
    
    
    public IStorage GetStorage(string tenant) =>
        _storages.GetOrAdd(
            tenant, 
            CreateStorage
        );

    private Storage CreateStorage(string tenant) =>
        new Storage(
            Path.Combine(_options.Value.ResolvedBasePath, tenant, "data"),
            _loggerFactory.CreateLogger<Storage>());

}
