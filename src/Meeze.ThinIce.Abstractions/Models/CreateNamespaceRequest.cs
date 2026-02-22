using System.Text.Json.Serialization;

namespace Meeze.ThinIce.Models;

public record CreateNamespaceRequest(
    [property: JsonPropertyName("namespace")] string[] Namespace,
    [property: JsonPropertyName("properties")] Dictionary<string, string>? Properties = null);
