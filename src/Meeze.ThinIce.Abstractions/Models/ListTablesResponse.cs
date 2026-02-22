using System.Text.Json.Serialization;

namespace Meeze.ThinIce.Models;

public record ListTablesResponse(
    [property: JsonPropertyName("identifiers")] TableIdentifier[] Identifiers);
