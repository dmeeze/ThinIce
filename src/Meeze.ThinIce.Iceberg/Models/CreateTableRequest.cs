using System.Text.Json.Serialization;

namespace Meeze.ThinIce.Iceberg.Models;

public record CreateTableRequest(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("schema")] Schema Schema,
    [property: JsonPropertyName("partition-spec")] PartitionSpec? PartitionSpec = null,
    [property: JsonPropertyName("sort-order")] SortOrder? SortOrder = null,
    [property: JsonPropertyName("properties")] Dictionary<string, string>? Properties = null);
