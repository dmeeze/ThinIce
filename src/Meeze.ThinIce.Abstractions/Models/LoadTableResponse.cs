using System.Text.Json.Serialization;

namespace Meeze.ThinIce.Iceberg.Models;

public record LoadTableResponse(
    [property: JsonPropertyName("metadata")] TableMetadata Metadata,
    [property: JsonPropertyName("metadata-location")] string MetadataLocation);
