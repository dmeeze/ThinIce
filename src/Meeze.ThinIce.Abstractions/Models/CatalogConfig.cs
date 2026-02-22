using System.Text.Json.Serialization;

namespace Meeze.ThinIce.Models;

public record CatalogConfig(
    [property: JsonPropertyName("defaults")] Dictionary<string, string>? Defaults = null,
    [property: JsonPropertyName("overrides")] Dictionary<string, string>? Overrides = null);
