using System.Text.Json.Serialization;
using Meeze.ThinIce.Auth.Models;
using Meeze.ThinIce.Iceberg.Models;

namespace Meeze.ThinIce.App;

[JsonSerializable(typeof(OAuthTokenResponse))]
[JsonSerializable(typeof(CatalogConfig))]
[JsonSerializable(typeof(IcebergErrorResponse))]
[JsonSerializable(typeof(ListNamespacesResponse))]
[JsonSerializable(typeof(NamespaceDetail))]
[JsonSerializable(typeof(CreateNamespaceRequest))]
[JsonSerializable(typeof(ListTablesResponse))]
[JsonSerializable(typeof(LoadTableResponse))]
[JsonSerializable(typeof(CreateTableRequest))]
internal partial class AppJsonSerializerContext : JsonSerializerContext;
