using System.Text.Json.Serialization;

namespace Meeze.ThinIce.Models;

public record IcebergErrorResponse(
    [property: JsonPropertyName("error")] IcebergError Error);

public record IcebergError(
    [property: JsonPropertyName("message")] string Message,
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("code")] int Code);
