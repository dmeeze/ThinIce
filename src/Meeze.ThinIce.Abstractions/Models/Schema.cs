using System.Text.Json.Serialization;

namespace Meeze.ThinIce.Models;

public record Schema(
    [property: JsonPropertyName("schema-id")] int SchemaId,
    [property: JsonPropertyName("fields")] SchemaField[] Fields,
    [property: JsonPropertyName("type")] string Type = "struct");

public record SchemaField(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("required")] bool Required,
    [property: JsonPropertyName("type")] string Type);
