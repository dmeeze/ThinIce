using Meeze.ThinIce.Iceberg.Models;

namespace Meeze.ThinIce.Iceberg;

public interface IIcebergCatalog
{
    Task<ListNamespacesResponse> ListNamespacesAsync(CancellationToken ct = default);
    Task<NamespaceDetail> CreateNamespaceAsync(CreateNamespaceRequest request, CancellationToken ct = default);
    Task<NamespaceDetail> LoadNamespaceAsync(string[] namespaceLevels, CancellationToken ct = default);
    Task DropNamespaceAsync(string[] namespaceLevels, CancellationToken ct = default);

    Task<ListTablesResponse> ListTablesAsync(string[] namespaceLevels, CancellationToken ct = default);
    Task<LoadTableResponse> CreateTableAsync(string[] namespaceLevels, CreateTableRequest request, CancellationToken ct = default);
    Task<LoadTableResponse> LoadTableAsync(string[] namespaceLevels, string table, CancellationToken ct = default);
    Task DropTableAsync(string[] namespaceLevels, string table, CancellationToken ct = default);
}
