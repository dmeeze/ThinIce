using System.Text.Json;
using Microsoft.Extensions.Logging;
using Meeze.ThinIce.Models;

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

    public Task<LoadTableResponse> CommitTableAsync(string[] namespaceLevels, string table, CommitTableRequest request, CancellationToken ct = default)
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

            var metadataDir = TableMetadataDir(namespaceLevels, table);
            var metadataFiles = Directory.GetFiles(metadataDir, "v*.metadata.json");

            var latestFile = metadataFiles
                .OrderByDescending(ParseMetadataVersion)
                .First();

            using var stream = File.OpenRead(latestFile);
            var metadata = JsonSerializer.Deserialize(stream, JsonContext.Default.TableMetadata)!;
            stream.Close();

            // Validate requirements
            foreach (var requirement in request.Requirements)
            {
                switch (requirement)
                {
                    case AssertCreate:
                        throw new InvalidOperationException("Table already exists");
                    case AssertTableUUID assert when assert.Uuid != metadata.TableUuid:
                        throw new InvalidOperationException($"Table UUID mismatch: expected {assert.Uuid}, got {metadata.TableUuid}");
                    case AssertCurrentSchemaId assert when assert.CurrentSchemaId != metadata.CurrentSchemaId:
                        throw new InvalidOperationException($"Current schema ID mismatch: expected {assert.CurrentSchemaId}, got {metadata.CurrentSchemaId}");
                }
            }

            // Apply updates
            var updatedMetadata = metadata;
            var properties = metadata.Properties != null ? new Dictionary<string, string>(metadata.Properties) : new Dictionary<string, string>();

            foreach (var update in request.Updates)
            {
                switch (update)
                {
                    case AssignUUIDUpdate assign:
                        updatedMetadata = updatedMetadata with { TableUuid = assign.Uuid };
                        break;
                    case UpgradeFormatVersionUpdate upgrade:
                        updatedMetadata = updatedMetadata with { FormatVersion = upgrade.FormatVersion };
                        break;
                    case SetPropertiesUpdate setProps:
                        foreach (var (key, value) in setProps.Updates)
                            properties[key] = value;
                        break;
                    case RemovePropertiesUpdate removeProps:
                        foreach (var key in removeProps.Removals)
                            properties.Remove(key);
                        break;
                    case SetLocationUpdate setLocation:
                        updatedMetadata = updatedMetadata with { Location = setLocation.Location };
                        break;
                    case AddSnapshotUpdate addSnapshot:
                        var snapshots = metadata.Snapshots?.ToList() ?? [];
                        snapshots.Add(addSnapshot.Snapshot);
                        updatedMetadata = updatedMetadata with
                        {
                            Snapshots = snapshots.ToArray(),
                            CurrentSnapshotId = addSnapshot.Snapshot.SnapshotId,
                            LastUpdatedMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                        };
                        break;
                }
            }

            updatedMetadata = updatedMetadata with
            {
                Properties = properties,
                LastUpdatedMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };

            // Write new metadata version
            var currentVersion = ParseMetadataVersion(latestFile);
            var newVersion = currentVersion + 1;
            var newMetadataFile = Path.Combine(metadataDir, $"v{newVersion}.metadata.json");

            using var writeStream = File.Create(newMetadataFile);
            JsonSerializer.Serialize(writeStream, updatedMetadata, JsonContext.Default.TableMetadata);

            LogTableCommitted(table, string.Join(".", namespaceLevels), newVersion);
            return Task.FromResult(new LoadTableResponse(updatedMetadata, newMetadataFile));
        }
        finally
        {
            _lock.ExitWriteLock();
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

    public Task RenameTableAsync(TableIdentifier source, TableIdentifier destination, CancellationToken ct = default)
    {
        _lock.EnterWriteLock();
        try
        {
            var sourceNsPath = NamespacePath(source.Namespace);
            if (!Directory.Exists(sourceNsPath))
                throw new DirectoryNotFoundException($"Source namespace not found: {string.Join(".", source.Namespace)}");

            var destNsPath = NamespacePath(destination.Namespace);
            if (!Directory.Exists(destNsPath))
                throw new DirectoryNotFoundException($"Destination namespace not found: {string.Join(".", destination.Namespace)}");

            var sourceTableDir = TablePath(source.Namespace, source.Name);
            if (!Directory.Exists(sourceTableDir))
                throw new FileNotFoundException($"Source table not found: {source.Name}");

            var destTableDir = TablePath(destination.Namespace, destination.Name);
            if (Directory.Exists(destTableDir))
                throw new InvalidOperationException($"Destination table already exists: {destination.Name}");

            Directory.Move(sourceTableDir, destTableDir);
            LogTableRenamed(source.Name, string.Join(".", source.Namespace), destination.Name, string.Join(".", destination.Namespace));
            return Task.CompletedTask;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public Task<LoadTableResponse> RegisterTableAsync(string[] namespaceLevels, string name, string metadataLocation, CancellationToken ct = default)
    {
        _lock.EnterWriteLock();
        try
        {
            var nsPath = NamespacePath(namespaceLevels);
            if (!Directory.Exists(nsPath))
                throw new DirectoryNotFoundException($"Namespace not found: {string.Join(".", namespaceLevels)}");

            var tableDir = TablePath(namespaceLevels, name);
            if (Directory.Exists(tableDir))
                throw new InvalidOperationException($"Table already exists: {name}");

            if (!File.Exists(metadataLocation))
                throw new FileNotFoundException($"Metadata file not found: {metadataLocation}");

            using var stream = File.OpenRead(metadataLocation);
            var metadata = JsonSerializer.Deserialize(stream, JsonContext.Default.TableMetadata)!;

            LogTableRegistered(name, string.Join(".", namespaceLevels), metadataLocation);
            return Task.FromResult(new LoadTableResponse(metadata, metadataLocation));
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public Task<bool> TableExistsAsync(string[] namespaceLevels, string table, CancellationToken ct = default)
    {
        _lock.EnterReadLock();
        try
        {
            var nsPath = NamespacePath(namespaceLevels);
            if (!Directory.Exists(nsPath))
                return Task.FromResult(false);

            var tableDir = TablePath(namespaceLevels, table);
            return Task.FromResult(Directory.Exists(tableDir));
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Listed {Count} tables in namespace {Namespace}")]
    private partial void LogTablesListed(int count, string @namespace);

    [LoggerMessage(Level = LogLevel.Information, Message = "Created table {Table} in namespace {Namespace}")]
    private partial void LogTableCreated(string table, string @namespace);

    [LoggerMessage(Level = LogLevel.Information, Message = "Committed changes to table {Table} in namespace {Namespace}, new version {Version}")]
    private partial void LogTableCommitted(string table, string @namespace, int version);

    [LoggerMessage(Level = LogLevel.Information, Message = "Renamed table {SourceTable} from namespace {SourceNamespace} to {DestTable} in namespace {DestNamespace}")]
    private partial void LogTableRenamed(string sourceTable, string sourceNamespace, string destTable, string destNamespace);

    [LoggerMessage(Level = LogLevel.Information, Message = "Registered table {Table} in namespace {Namespace} from metadata location {MetadataLocation}")]
    private partial void LogTableRegistered(string table, string @namespace, string metadataLocation);

    [LoggerMessage(Level = LogLevel.Information, Message = "Dropped table {Table} from namespace {Namespace}")]
    private partial void LogTableDropped(string table, string @namespace);
}
