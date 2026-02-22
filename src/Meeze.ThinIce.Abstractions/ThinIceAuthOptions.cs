namespace Meeze.ThinIce;

public sealed class ThinIceAuthOptions
{
    public List<AnonymousEndpoint> AnonymousEndpoints { get; set; } = [];
}

public sealed record AnonymousEndpoint(string Path, string? Method = null);
