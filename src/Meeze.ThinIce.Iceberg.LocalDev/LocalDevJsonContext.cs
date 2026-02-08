using System.Text.Json.Serialization;

namespace Meeze.ThinIce.Iceberg.LocalDev;

[JsonSerializable(typeof(Dictionary<string, string>))]
internal partial class LocalDevJsonContext : JsonSerializerContext;
