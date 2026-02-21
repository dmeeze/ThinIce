using Microsoft.Extensions.Logging;

namespace Meeze.ThinIce.Iceberg.LocalDev;

public sealed partial class Catalog : ICatalog, IDisposable
{
    private readonly string _namespacesRoot;
    private readonly ILogger<Catalog> _logger;
    private readonly ReaderWriterLockSlim _lock = new();

    public Catalog(string namespacesRoot, ILogger<Catalog> logger)
    {
        _namespacesRoot = namespacesRoot;
        _logger = logger;
    }

    private string NamespacePath(string[] levels) =>
        Path.Combine(_namespacesRoot, NamespaceHelpers.Encode(levels));

    public void Dispose() => _lock.Dispose();
}
