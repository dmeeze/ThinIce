using System.Text.Json;
using Microsoft.Extensions.Logging;
using Meeze.ThinIce.Iceberg.Models;

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

    public Task<ListNamespacesResponse> ListNamespacesAsync(CancellationToken ct = default)
    {
        _lock.EnterReadLock();
        try
        {
            if (!Directory.Exists(_namespacesRoot))
                return Task.FromResult(new ListNamespacesResponse([]));

            var dirs = Directory.GetDirectories(_namespacesRoot);
            var namespaces = dirs
                .Select(d => NamespaceHelpers.Parse(Path.GetFileName(d)))
                .ToArray();

            var result = new ListNamespacesResponse(namespaces);
            LogNamespacesListed(result.Namespaces.Length);
            return Task.FromResult(result);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public Task<NamespaceDetail> CreateNamespaceAsync(CreateNamespaceRequest request, CancellationToken ct = default)
    {
        _lock.EnterWriteLock();
        try
        {
            var nsPath = NamespacePath(request.Namespace);
            if (Directory.Exists(nsPath))
                throw new InvalidOperationException($"Namespace already exists: {string.Join(".", request.Namespace)}");

            Directory.CreateDirectory(nsPath);

            var properties = request.Properties ?? new Dictionary<string, string>();
            var propsFile = Path.Combine(nsPath, "properties.json");
            using var stream = File.Create(propsFile);
            JsonSerializer.Serialize(stream, properties, LocalDevJsonContext.Default.DictionaryStringString);

            var result = new NamespaceDetail(request.Namespace, properties);
            LogNamespaceCreated(string.Join(".", request.Namespace));
            return Task.FromResult(result);
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public Task<NamespaceDetail> LoadNamespaceAsync(string[] namespaceLevels, CancellationToken ct = default)
    {
        _lock.EnterReadLock();
        try
        {
            var nsPath = NamespacePath(namespaceLevels);
            if (!Directory.Exists(nsPath))
                throw new DirectoryNotFoundException($"Namespace not found: {string.Join(".", namespaceLevels)}");

            var propsFile = Path.Combine(nsPath, "properties.json");
            Dictionary<string, string>? properties = null;
            if (File.Exists(propsFile))
            {
                using var stream = File.OpenRead(propsFile);
                properties = JsonSerializer.Deserialize(stream, LocalDevJsonContext.Default.DictionaryStringString);
            }

            return Task.FromResult(new NamespaceDetail(namespaceLevels, properties));
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public Task DropNamespaceAsync(string[] namespaceLevels, CancellationToken ct = default)
    {
        _lock.EnterWriteLock();
        try
        {
            var nsPath = NamespacePath(namespaceLevels);
            if (!Directory.Exists(nsPath))
                throw new DirectoryNotFoundException($"Namespace not found: {string.Join(".", namespaceLevels)}");

            Directory.Delete(nsPath, recursive: true);
            LogNamespaceDropped(string.Join(".", namespaceLevels));
            return Task.CompletedTask;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    // Table operations — Phase 4
    public Task<ListTablesResponse> ListTablesAsync(string[] namespaceLevels, CancellationToken ct = default) =>
        throw new NotImplementedException();

    public Task<LoadTableResponse> CreateTableAsync(string[] namespaceLevels, CreateTableRequest request, CancellationToken ct = default) =>
        throw new NotImplementedException();

    public Task<LoadTableResponse> LoadTableAsync(string[] namespaceLevels, string table, CancellationToken ct = default) =>
        throw new NotImplementedException();

    public Task DropTableAsync(string[] namespaceLevels, string table, CancellationToken ct = default) =>
        throw new NotImplementedException();

    public void Dispose() => _lock.Dispose();

    [LoggerMessage(Level = LogLevel.Debug, Message = "Listed {Count} namespaces")]
    private partial void LogNamespacesListed(int count);

    [LoggerMessage(Level = LogLevel.Information, Message = "Created namespace {Namespace}")]
    private partial void LogNamespaceCreated(string @namespace);

    [LoggerMessage(Level = LogLevel.Information, Message = "Dropped namespace {Namespace}")]
    private partial void LogNamespaceDropped(string @namespace);
}
