using System.Text.Json.Serialization;

namespace Meeze.ThinIce.Models;

public record TableMetadata(
    [property: JsonPropertyName("format-version")] int FormatVersion,
    [property: JsonPropertyName("table-uuid")] string TableUuid,
    [property: JsonPropertyName("location")] string Location,
    [property: JsonPropertyName("last-updated-ms")] long LastUpdatedMs,
    [property: JsonPropertyName("schemas")] Schema[] Schemas,
    [property: JsonPropertyName("current-schema-id")] int CurrentSchemaId,
    [property: JsonPropertyName("partition-specs")] PartitionSpec[] PartitionSpecs,
    [property: JsonPropertyName("default-spec-id")] int DefaultSpecId,
    [property: JsonPropertyName("sort-orders")] SortOrder[] SortOrders,
    [property: JsonPropertyName("default-sort-order-id")] int DefaultSortOrderId,
    [property: JsonPropertyName("current-snapshot-id")] long CurrentSnapshotId,
    [property: JsonPropertyName("last-sequence-number")] long LastSequenceNumber,
    [property: JsonPropertyName("properties")] Dictionary<string, string>? Properties = null,
    [property: JsonPropertyName("snapshots")] Snapshot[]? Snapshots = null);
