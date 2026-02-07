using Meeze.ThinIce.Iceberg.Models;

namespace Meeze.ThinIce.Iceberg;

public interface IIcebergCatalog
{
    Task<ListNamespacesResponse> ListNamespacesAsync(string tenant, CancellationToken ct = default);
    Task<NamespaceDetail> CreateNamespaceAsync(string tenant, CreateNamespaceRequest request, CancellationToken ct = default);
    Task<NamespaceDetail> LoadNamespaceAsync(string tenant, string[] namespaceLevels, CancellationToken ct = default);
    Task DropNamespaceAsync(string tenant, string[] namespaceLevels, CancellationToken ct = default);

    Task<ListTablesResponse> ListTablesAsync(string tenant, string[] namespaceLevels, CancellationToken ct = default);
    Task<LoadTableResponse> CreateTableAsync(string tenant, string[] namespaceLevels, CreateTableRequest request, CancellationToken ct = default);
    Task<LoadTableResponse> LoadTableAsync(string tenant, string[] namespaceLevels, string table, CancellationToken ct = default);
    Task DropTableAsync(string tenant, string[] namespaceLevels, string table, CancellationToken ct = default);
}
