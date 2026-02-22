using System.Text.Json.Serialization;
using Meeze.ThinIce.Models;

namespace Meeze.ThinIce;

[JsonSerializable(typeof(IcebergErrorResponse))]
public partial class AuthJsonContext : JsonSerializerContext;
