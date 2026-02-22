using System.Text.Json.Serialization;

namespace Meeze.ThinIce.Models;

public sealed record RegisterTableRequest(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("metadata-location")] string MetadataLocation
);
