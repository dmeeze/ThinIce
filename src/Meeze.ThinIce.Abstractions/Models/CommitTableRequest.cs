using System.Text.Json.Serialization;

namespace Meeze.ThinIce.Iceberg.Models;

/// <summary>
/// Request to commit updates to a table.
/// </summary>
public sealed record CommitTableRequest(
    [property: JsonPropertyName("identifier")] TableIdentifier? Identifier,
    [property: JsonPropertyName("requirements")] TableRequirement[] Requirements,
    [property: JsonPropertyName("updates")] TableUpdate[] Updates
);

/// <summary>
/// Base class for table requirements (assertions that must pass before applying updates).
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(AssertCreate), "assert-create")]
[JsonDerivedType(typeof(AssertTableUUID), "assert-table-uuid")]
[JsonDerivedType(typeof(AssertRefSnapshotId), "assert-ref-snapshot-id")]
[JsonDerivedType(typeof(AssertCurrentSchemaId), "assert-current-schema-id")]
public abstract record TableRequirement;

public sealed record AssertCreate() : TableRequirement;

public sealed record AssertTableUUID(
    [property: JsonPropertyName("uuid")] string Uuid
) : TableRequirement;

public sealed record AssertRefSnapshotId(
    [property: JsonPropertyName("ref")] string Ref,
    [property: JsonPropertyName("snapshot-id")] long? SnapshotId
) : TableRequirement;

public sealed record AssertCurrentSchemaId(
    [property: JsonPropertyName("current-schema-id")] int CurrentSchemaId
) : TableRequirement;

/// <summary>
/// Base class for table updates (changes to apply to table metadata).
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "action")]
[JsonDerivedType(typeof(AssignUUIDUpdate), "assign-uuid")]
[JsonDerivedType(typeof(UpgradeFormatVersionUpdate), "upgrade-format-version")]
[JsonDerivedType(typeof(AddSnapshotUpdate), "add-snapshot")]
[JsonDerivedType(typeof(SetPropertiesUpdate), "set-properties")]
[JsonDerivedType(typeof(RemovePropertiesUpdate), "remove-properties")]
[JsonDerivedType(typeof(SetLocationUpdate), "set-location")]
public abstract record TableUpdate;

public sealed record AssignUUIDUpdate(
    [property: JsonPropertyName("uuid")] string Uuid
) : TableUpdate;

public sealed record UpgradeFormatVersionUpdate(
    [property: JsonPropertyName("format-version")] int FormatVersion
) : TableUpdate;

public sealed record AddSnapshotUpdate(
    [property: JsonPropertyName("snapshot")] Snapshot Snapshot
) : TableUpdate;

public sealed record SetPropertiesUpdate(
    [property: JsonPropertyName("updates")] Dictionary<string, string> Updates
) : TableUpdate;

public sealed record RemovePropertiesUpdate(
    [property: JsonPropertyName("removals")] string[] Removals
) : TableUpdate;

public sealed record SetLocationUpdate(
    [property: JsonPropertyName("location")] string Location
) : TableUpdate;
