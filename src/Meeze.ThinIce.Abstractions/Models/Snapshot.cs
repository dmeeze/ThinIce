using System.Text.Json.Serialization;

namespace Meeze.ThinIce.Models;

public record Snapshot(
    [property: JsonPropertyName("snapshot-id")] long SnapshotId,
    [property: JsonPropertyName("timestamp-ms")] long TimestampMs,
    [property: JsonPropertyName("manifest-list")] string ManifestList,
    [property: JsonPropertyName("summary")] Dictionary<string, string>? Summary = null);
