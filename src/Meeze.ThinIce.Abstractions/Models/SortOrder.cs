using System.Text.Json.Serialization;

namespace Meeze.ThinIce.Iceberg.Models;

public record SortOrder(
    [property: JsonPropertyName("order-id")] int OrderId,
    [property: JsonPropertyName("fields")] SortField[] Fields);

public record SortField(
    [property: JsonPropertyName("source-id")] int SourceId,
    [property: JsonPropertyName("transform")] string Transform,
    [property: JsonPropertyName("direction")] string Direction,
    [property: JsonPropertyName("null-order")] string NullOrder);
