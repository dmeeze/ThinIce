namespace Meeze.ThinIce.Iceberg.LocalDev;

public sealed class Options
{
    public string? BasePath { get; set; }
    public List<string> Tokens { get; set; } = [];

    public string ResolvedBasePath =>
        BasePath ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ThinIce", "data");
}
