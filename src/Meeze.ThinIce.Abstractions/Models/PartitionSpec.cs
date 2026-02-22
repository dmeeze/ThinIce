using System.Text.Json.Serialization;

namespace Meeze.ThinIce.Models;

public record PartitionSpec(
    [property: JsonPropertyName("spec-id")] int SpecId,
    [property: JsonPropertyName("fields")] PartitionField[] Fields);

public record PartitionField(
    [property: JsonPropertyName("source-id")] int SourceId,
    [property: JsonPropertyName("field-id")] int FieldId,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("transform")] string Transform);
