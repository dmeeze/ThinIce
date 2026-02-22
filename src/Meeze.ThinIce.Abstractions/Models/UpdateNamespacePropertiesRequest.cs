using System.Text.Json.Serialization;

namespace Meeze.ThinIce.Iceberg.Models;

public sealed record UpdateNamespacePropertiesRequest(
    [property: JsonPropertyName("updates")] Dictionary<string, string>? Updates,
    [property: JsonPropertyName("removals")] string[]? Removals
);
