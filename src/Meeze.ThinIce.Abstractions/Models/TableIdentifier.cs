using System.Text.Json.Serialization;

namespace Meeze.ThinIce.Iceberg.Models;

public record TableIdentifier(
    [property: JsonPropertyName("namespace")] string[] Namespace,
    [property: JsonPropertyName("name")] string Name);
