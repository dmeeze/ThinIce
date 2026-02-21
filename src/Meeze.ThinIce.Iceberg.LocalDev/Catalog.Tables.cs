using System.Text.Json;
using Microsoft.Extensions.Logging;
using Meeze.ThinIce.Iceberg.Models;

namespace Meeze.ThinIce.Iceberg.LocalDev;

public sealed partial class Catalog
{
    private string TablesPath(string[] namespaceLevels) =>
        Path.Combine(NamespacePath(namespaceLevels), "tables");

    private string TablePath(string[] namespaceLevels, string tableName) =>
        Path.Combine(TablesPath(namespaceLevels), tableName);

    private string TableMetadataDir(string[] namespaceLevels, string tableName) =>
        Path.Combine(TablePath(namespaceLevels, tableName), "metadata");

    private static int ParseMetadataVersion(string fileName)
    {
        // v1.metadata.json → 1
        var span = Path.GetFileName(fileName).AsSpan();
        if (span.Length > 0 && span[0] == 'v')
        {
            var dotIndex = span.Slice(1).IndexOf('.');
            if (dotIndex > 0 && int.TryParse(span.Slice(1, dotIndex), out var version))
                return version;
        }
        return -1;
    }

    public Task<ListTablesResponse> ListTablesAsync(string[] namespaceLevels, CancellationToken ct = default)
    {
        _lock.EnterReadLock();
        try
        {
            var nsPath = NamespacePath(namespaceLevels);
            if (!Directory.Exists(nsPath))
                throw new DirectoryNotFoundException($"Namespace not found: {string.Join(".", namespaceLevels)}");

            var tablesDir = TablesPath(namespaceLevels);
            if (!Directory.Exists(tablesDir))
                return Task.FromResult(new ListTablesResponse([]));

            var identifiers = Directory.GetDirectories(tablesDir)
                .Select(d => new TableIdentifier(namespaceLevels, Path.GetFileName(d)))
                .ToArray();

            LogTablesListed(identifiers.Length, string.Join(".", namespaceLevels));
            return Task.FromResult(new ListTablesResponse(identifiers));
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public Task<LoadTableResponse> CreateTableAsync(string[] namespaceLevels, CreateTableRequest request, CancellationToken ct = default)
    {
        _lock.EnterWriteLock();
        try
        {
            var nsPath = NamespacePath(namespaceLevels);
            if (!Directory.Exists(nsPath))
                throw new DirectoryNotFoundException($"Namespace not found: {string.Join(".", namespaceLevels)}");

            var tableDir = TablePath(namespaceLevels, request.Name);
            if (Directory.Exists(tableDir))
                throw new InvalidOperationException($"Table already exists: {request.Name}");

            var metadataDir = TableMetadataDir(namespaceLevels, request.Name);
            Directory.CreateDirectory(metadataDir);

            var partitionSpec = request.PartitionSpec ?? new PartitionSpec(0, []);
            var sortOrder = request.SortOrder ?? new SortOrder(0, []);

            var metadata = new TableMetadata(
                FormatVersion: 2,
                TableUuid: Guid.NewGuid().ToString(),
                Location: tableDir,
                LastUpdatedMs: DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                Schemas: [request.Schema],
                CurrentSchemaId: 0,
                PartitionSpecs: [partitionSpec],
                DefaultSpecId: partitionSpec.SpecId,
                SortOrders: [sortOrder],
                DefaultSortOrderId: sortOrder.OrderId,
                CurrentSnapshotId: -1,
                LastSequenceNumber: 0,
                Properties: request.Properties);

            var metadataFile = Path.Combine(metadataDir, "v1.metadata.json");
            using var stream = File.Create(metadataFile);
            JsonSerializer.Serialize(stream, metadata, JsonContext.Default.TableMetadata);

            LogTableCreated(request.Name, string.Join(".", namespaceLevels));
            return Task.FromResult(new LoadTableResponse(metadata, metadataFile));
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public Task<LoadTableResponse> LoadTableAsync(string[] namespaceLevels, string table, CancellationToken ct = default)
    {
        _lock.EnterReadLock();
        try
        {
            var nsPath = NamespacePath(namespaceLevels);
            if (!Directory.Exists(nsPath))
                throw new DirectoryNotFoundException($"Namespace not found: {string.Join(".", namespaceLevels)}");

            var tableDir = TablePath(namespaceLevels, table);
            if (!Directory.Exists(tableDir))
                throw new FileNotFoundException($"Table not found: {table}");

            var metadataDir = TableMetadataDir(namespaceLevels, table);
            var metadataFiles = Directory.GetFiles(metadataDir, "v*.metadata.json");

            var latestFile = metadataFiles
                .OrderByDescending(ParseMetadataVersion)
                .First();

            using var stream = File.OpenRead(latestFile);
            var metadata = JsonSerializer.Deserialize(stream, JsonContext.Default.TableMetadata)!;

            return Task.FromResult(new LoadTableResponse(metadata, latestFile));
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public Task DropTableAsync(string[] namespaceLevels, string table, CancellationToken ct = default)
    {
        _lock.EnterWriteLock();
        try
        {
            var nsPath = NamespacePath(namespaceLevels);
            if (!Directory.Exists(nsPath))
                throw new DirectoryNotFoundException($"Namespace not found: {string.Join(".", namespaceLevels)}");

            var tableDir = TablePath(namespaceLevels, table);
            if (!Directory.Exists(tableDir))
                throw new FileNotFoundException($"Table not found: {table}");

            Directory.Delete(tableDir, recursive: true);
            LogTableDropped(table, string.Join(".", namespaceLevels));
            return Task.CompletedTask;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Listed {Count} tables in namespace {Namespace}")]
    private partial void LogTablesListed(int count, string @namespace);

    [LoggerMessage(Level = LogLevel.Information, Message = "Created table {Table} in namespace {Namespace}")]
    private partial void LogTableCreated(string table, string @namespace);

    [LoggerMessage(Level = LogLevel.Information, Message = "Dropped table {Table} from namespace {Namespace}")]
    private partial void LogTableDropped(string table, string @namespace);
}
