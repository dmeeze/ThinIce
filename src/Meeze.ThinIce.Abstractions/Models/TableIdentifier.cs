using System.Text.Json.Serialization;

namespace Meeze.ThinIce.Models;

public record TableIdentifier(
    [property: JsonPropertyName("namespace")] string[] Namespace,
    [property: JsonPropertyName("name")] string Name);
