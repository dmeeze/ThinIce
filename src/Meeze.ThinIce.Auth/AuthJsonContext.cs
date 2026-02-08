using System.Text.Json.Serialization;
using Meeze.ThinIce.Iceberg.Models;

namespace Meeze.ThinIce.Auth;

[JsonSerializable(typeof(IcebergErrorResponse))]
internal partial class AuthJsonContext : JsonSerializerContext;
