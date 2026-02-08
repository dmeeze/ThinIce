using System.Text.Json.Serialization;

namespace Meeze.ThinIce.Iceberg.Models;

public record ListNamespacesResponse(
    [property: JsonPropertyName("namespaces")] string[][] Namespaces);
