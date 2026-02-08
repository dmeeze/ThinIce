using Microsoft.Extensions.Logging;

namespace Meeze.ThinIce.Iceberg.LocalDev;

public sealed partial class LocalDevCatalog : IIcebergCatalog, IDisposable
{
    private readonly string _namespacesRoot;
    private readonly ILogger<LocalDevCatalog> _logger;
    private readonly ReaderWriterLockSlim _lock = new();

    public LocalDevCatalog(string namespacesRoot, ILogger<LocalDevCatalog> logger)
    {
        _namespacesRoot = namespacesRoot;
        _logger = logger;
    }

    private string NamespacePath(string[] levels) =>
        Path.Combine(_namespacesRoot, NamespaceHelpers.Encode(levels));

    public void Dispose() => _lock.Dispose();
}
