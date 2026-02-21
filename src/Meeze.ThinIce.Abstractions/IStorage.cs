namespace Meeze.ThinIce.Iceberg;

public interface IStorage
{
    Task<Stream> ReadFileAsync(string path, CancellationToken ct = default);
    Task WriteFileAsync(string path, Stream content, CancellationToken ct = default);
    Task DeleteFileAsync(string path, CancellationToken ct = default);
}
