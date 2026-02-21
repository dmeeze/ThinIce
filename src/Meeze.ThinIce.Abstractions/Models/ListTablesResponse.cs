using System.Text.Json.Serialization;

namespace Meeze.ThinIce.Iceberg.Models;

public record ListTablesResponse(
    [property: JsonPropertyName("identifiers")] TableIdentifier[] Identifiers);
