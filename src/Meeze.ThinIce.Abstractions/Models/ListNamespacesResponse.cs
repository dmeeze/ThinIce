using System.Text.Json.Serialization;

namespace Meeze.ThinIce.Models;

public record ListNamespacesResponse(
    [property: JsonPropertyName("namespaces")] string[][] Namespaces);
