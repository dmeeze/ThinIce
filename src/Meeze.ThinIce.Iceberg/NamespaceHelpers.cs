namespace Meeze.ThinIce.Iceberg;

/// <summary>
/// Iceberg REST spec encodes multi-level namespaces with \x1F (unit separator) in URL paths.
/// </summary>
public static class NamespaceHelpers
{
    private const char Separator = '\u001F';

    public static string[] Parse(string encoded) =>
        encoded.Split(Separator);

    public static string Encode(string[] levels) =>
        string.Join(Separator, levels);
}
