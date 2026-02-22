using System.Text.Json.Serialization;
using Meeze.ThinIce.Models;

namespace Meeze.ThinIce.App;

[JsonSerializable(typeof(CatalogConfig))]
[JsonSerializable(typeof(IcebergErrorResponse))]
[JsonSerializable(typeof(ListNamespacesResponse))]
[JsonSerializable(typeof(NamespaceDetail))]
[JsonSerializable(typeof(CreateNamespaceRequest))]
[JsonSerializable(typeof(UpdateNamespacePropertiesRequest))]
[JsonSerializable(typeof(ListTablesResponse))]
[JsonSerializable(typeof(LoadTableResponse))]
[JsonSerializable(typeof(CreateTableRequest))]
[JsonSerializable(typeof(CommitTableRequest))]
[JsonSerializable(typeof(RenameTableRequest))]
[JsonSerializable(typeof(RegisterTableRequest))]
internal partial class AppJsonSerializerContext : JsonSerializerContext;
