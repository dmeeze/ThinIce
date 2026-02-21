using System.Text.Json.Serialization;
using Meeze.ThinIce.Iceberg.Models;

namespace Meeze.ThinIce.Iceberg.LocalDev;

[JsonSerializable(typeof(Dictionary<string, string>))]
[JsonSerializable(typeof(TableMetadata))]
internal partial class JsonContext : JsonSerializerContext;
