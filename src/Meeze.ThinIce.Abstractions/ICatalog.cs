using Meeze.ThinIce.Iceberg.Models;

namespace Meeze.ThinIce.Iceberg;

public interface ICatalog
{
    Task<ListNamespacesResponse> ListNamespacesAsync(CancellationToken ct = default);
    Task<NamespaceDetail> CreateNamespaceAsync(CreateNamespaceRequest request, CancellationToken ct = default);
    Task<NamespaceDetail> LoadNamespaceAsync(string[] namespaceLevels, CancellationToken ct = default);
    Task DropNamespaceAsync(string[] namespaceLevels, CancellationToken ct = default);

    Task<ListTablesResponse> ListTablesAsync(string[] namespaceLevels, CancellationToken ct = default);
    Task<LoadTableResponse> CreateTableAsync(string[] namespaceLevels, CreateTableRequest request, CancellationToken ct = default);
    Task<LoadTableResponse> LoadTableAsync(string[] namespaceLevels, string table, CancellationToken ct = default);
    Task<LoadTableResponse> CommitTableAsync(string[] namespaceLevels, string table, CommitTableRequest request, CancellationToken ct = default);
    Task RenameTableAsync(TableIdentifier source, TableIdentifier destination, CancellationToken ct = default);
    Task<LoadTableResponse> RegisterTableAsync(string[] namespaceLevels, string name, string metadataLocation, CancellationToken ct = default);
    Task DropTableAsync(string[] namespaceLevels, string table, CancellationToken ct = default);

    Task<NamespaceDetail> UpdateNamespacePropertiesAsync(string[] namespaceLevels, Dictionary<string, string>? updates, string[]? removals, CancellationToken ct = default);
    Task<bool> NamespaceExistsAsync(string[] namespaceLevels, CancellationToken ct = default);
    Task<bool> TableExistsAsync(string[] namespaceLevels, string table, CancellationToken ct = default);
}
