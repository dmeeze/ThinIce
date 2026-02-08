using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Meeze.ThinIce.Iceberg.LocalDev;

namespace Meeze.ThinIce.Tests;

[TestClass]
public class LocalDevStorageResolverTests
{
    private string _tempDir = null!;
    private LocalDevStorageResolver _resolver = null!;

    [TestInitialize]
    public void Setup()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"ThinIce_Test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);

        var options = Options.Create(new LocalDevOptions { BasePath = _tempDir });
        _resolver = new LocalDevStorageResolver(options, NullLoggerFactory.Instance);
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    [TestMethod]
    public async Task TenantIsolation_SeparateStoragePerTenant()
    {
        var snowyStorage = _resolver.GetStorage("SnowyConesIceCream");
        var wayneStorage = _resolver.GetStorage("WayneEnterprises");

        var snowyPayload = "Snowy's secret recipe"u8.ToArray();
        using (var stream = new MemoryStream(snowyPayload))
            await snowyStorage.WriteFileAsync("recipe.dat", stream);

        var waynePayload = "Wayne's freeze ray specs"u8.ToArray();
        using (var stream = new MemoryStream(waynePayload))
            await wayneStorage.WriteFileAsync("recipe.dat", stream);

        // Each tenant gets their own file
        await using var snowyRead = await snowyStorage.ReadFileAsync("recipe.dat");
        using var snowyResult = new MemoryStream();
        await snowyRead.CopyToAsync(snowyResult);
        CollectionAssert.AreEqual(snowyPayload, snowyResult.ToArray());

        await using var wayneRead = await wayneStorage.ReadFileAsync("recipe.dat");
        using var wayneResult = new MemoryStream();
        await wayneRead.CopyToAsync(wayneResult);
        CollectionAssert.AreEqual(waynePayload, wayneResult.ToArray());
    }

    [TestMethod]
    public void SameTenant_ReturnsCachedInstance()
    {
        var first = _resolver.GetStorage("SnowyConesIceCream");
        var second = _resolver.GetStorage("SnowyConesIceCream");

        Assert.AreSame(first, second);
    }

    [TestMethod]
    public void DifferentTenants_ReturnDifferentInstances()
    {
        var snowy = _resolver.GetStorage("SnowyConesIceCream");
        var wayne = _resolver.GetStorage("WayneEnterprises");

        Assert.AreNotSame(snowy, wayne);
    }
}
