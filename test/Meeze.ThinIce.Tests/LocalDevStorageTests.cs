using Microsoft.Extensions.Logging.Abstractions;
using Meeze.ThinIce.Iceberg.LocalDev;

namespace Meeze.ThinIce.Tests;

[TestClass]
public class LocalDevStorageTests
{
    private string _tempDir = null!;
    private string _dataRoot = null!;
    private LocalDevStorage _storage = null!;

    [TestInitialize]
    public void Setup()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"ThinIce_Test_{Guid.NewGuid():N}");
        _dataRoot = Path.Combine(_tempDir, "data");
        Directory.CreateDirectory(_tempDir);
        _storage = new LocalDevStorage(_dataRoot, NullLogger<LocalDevStorage>.Instance);
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    [TestMethod]
    public async Task WriteAndRead_RoundTrip_ByteIdentical()
    {
        var payload = "Mr Freeze keeps his ice cream at -40C"u8.ToArray();
        using (var writeStream = new MemoryStream(payload))
            await _storage.WriteFileAsync("frozen-assets/inventory.bin", writeStream);

        await using var readStream = await _storage.ReadFileAsync("frozen-assets/inventory.bin");
        using var result = new MemoryStream();
        await readStream.CopyToAsync(result);

        CollectionAssert.AreEqual(payload, result.ToArray());
    }

    [TestMethod]
    public async Task WriteFile_CreatesIntermediateDirectories()
    {
        var payload = "deep freeze"u8.ToArray();
        using (var writeStream = new MemoryStream(payload))
            await _storage.WriteFileAsync("level1/level2/level3/snowflake.dat", writeStream);

        await using var readStream = await _storage.ReadFileAsync("level1/level2/level3/snowflake.dat");
        using var result = new MemoryStream();
        await readStream.CopyToAsync(result);

        CollectionAssert.AreEqual(payload, result.ToArray());
    }

    [TestMethod]
    public async Task WriteFile_OverwritesExisting()
    {
        using (var v1 = new MemoryStream("version one"u8.ToArray()))
            await _storage.WriteFileAsync("glacier.dat", v1);

        var updated = "version two — colder"u8.ToArray();
        using (var v2 = new MemoryStream(updated))
            await _storage.WriteFileAsync("glacier.dat", v2);

        await using var readStream = await _storage.ReadFileAsync("glacier.dat");
        using var result = new MemoryStream();
        await readStream.CopyToAsync(result);

        CollectionAssert.AreEqual(updated, result.ToArray());
    }

    [TestMethod]
    public async Task ReadFile_NotFound_ThrowsFileNotFound()
    {
        await Assert.ThrowsExactlyAsync<FileNotFoundException>(
            () => _storage.ReadFileAsync("phantom-ice.dat"));
    }

    [TestMethod]
    public async Task DeleteFile_Exists_RemovesFile()
    {
        using (var writeStream = new MemoryStream("meltable"u8.ToArray()))
            await _storage.WriteFileAsync("ice-cube.dat", writeStream);

        await _storage.DeleteFileAsync("ice-cube.dat");

        await Assert.ThrowsExactlyAsync<FileNotFoundException>(
            () => _storage.ReadFileAsync("ice-cube.dat"));
    }

    [TestMethod]
    public async Task DeleteFile_NotFound_ThrowsFileNotFound()
    {
        await Assert.ThrowsExactlyAsync<FileNotFoundException>(
            () => _storage.DeleteFileAsync("already-melted.dat"));
    }

    [TestMethod]
    public async Task LargeFile_RoundTrip_ByteIdentical()
    {
        // 1 MB of frozen data
        var payload = new byte[1024 * 1024];
        Random.Shared.NextBytes(payload);

        using (var writeStream = new MemoryStream(payload))
            await _storage.WriteFileAsync("iceberg-chunk.bin", writeStream);

        await using var readStream = await _storage.ReadFileAsync("iceberg-chunk.bin");
        using var result = new MemoryStream();
        await readStream.CopyToAsync(result);

        CollectionAssert.AreEqual(payload, result.ToArray());
    }
}
