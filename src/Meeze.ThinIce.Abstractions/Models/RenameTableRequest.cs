using System.Text.Json.Serialization;

namespace Meeze.ThinIce.Iceberg.Models;

public sealed record RenameTableRequest(
    [property: JsonPropertyName("source")] TableIdentifier Source,
    [property: JsonPropertyName("destination")] TableIdentifier Destination
);
