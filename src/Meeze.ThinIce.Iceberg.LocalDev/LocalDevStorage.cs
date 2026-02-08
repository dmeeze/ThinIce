using Microsoft.Extensions.Logging;

namespace Meeze.ThinIce.Iceberg.LocalDev;

public sealed partial class LocalDevStorage : IIcebergStorage
{
    private readonly string _dataRoot;
    private readonly ILogger<LocalDevStorage> _logger;

    public LocalDevStorage(string dataRoot, ILogger<LocalDevStorage> logger)
    {
        _dataRoot = dataRoot;
        _logger = logger;
    }

    public Task<Stream> ReadFileAsync(string path, CancellationToken ct = default)
    {
        var fullPath = Path.Combine(_dataRoot, path);
        if (!File.Exists(fullPath))
            throw new FileNotFoundException($"Data file not found: {path}");

        LogFileRead(path);
        Stream stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096, useAsync: true);
        return Task.FromResult(stream);
    }

    public async Task WriteFileAsync(string path, Stream content, CancellationToken ct = default)
    {
        var fullPath = Path.Combine(_dataRoot, path);
        var dir = Path.GetDirectoryName(fullPath);
        if (dir is not null)
            Directory.CreateDirectory(dir);

        await using var fileStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 4096, useAsync: true);
        await content.CopyToAsync(fileStream, ct);

        LogFileWritten(path);
    }

    public Task DeleteFileAsync(string path, CancellationToken ct = default)
    {
        var fullPath = Path.Combine(_dataRoot, path);
        if (!File.Exists(fullPath))
            throw new FileNotFoundException($"Data file not found: {path}");

        File.Delete(fullPath);
        LogFileDeleted(path);
        return Task.CompletedTask;
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Read data file {Path}")]
    private partial void LogFileRead(string path);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Wrote data file {Path}")]
    private partial void LogFileWritten(string path);

    [LoggerMessage(Level = LogLevel.Information, Message = "Deleted data file {Path}")]
    private partial void LogFileDeleted(string path);
}
