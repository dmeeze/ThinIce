namespace Meeze.ThinIce.Iceberg;

public interface IIcebergStorage
{
    Task<Stream> ReadFileAsync(string tenant, string path, CancellationToken ct = default);
    Task WriteFileAsync(string tenant, string path, Stream content, CancellationToken ct = default);
    Task DeleteFileAsync(string tenant, string path, CancellationToken ct = default);
}
