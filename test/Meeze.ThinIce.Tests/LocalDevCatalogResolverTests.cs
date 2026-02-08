using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Meeze.ThinIce.Iceberg.LocalDev;
using Meeze.ThinIce.Iceberg.Models;

namespace Meeze.ThinIce.Tests;

[TestClass]
public class LocalDevCatalogResolverTests
{
    private string _tempDir = null!;
    private LocalDevCatalogResolver _resolver = null!;

    [TestInitialize]
    public void Setup()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"ThinIce_Test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
        var options = Options.Create(new LocalDevOptions { BasePath = _tempDir });
        _resolver = new LocalDevCatalogResolver(options, NullLoggerFactory.Instance);
    }

    [TestCleanup]
    public void Cleanup()
    {
        _resolver.Dispose();
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    [TestMethod]
    public async Task TenantIsolation_NamespacesAreSeparate()
    {
        var snowyCatalog = _resolver.GetCatalog("SnowyConesIceCream");
        var wayneCatalog = _resolver.GetCatalog("WayneEnterprises");

        await snowyCatalog.CreateNamespaceAsync(new CreateNamespaceRequest(["shared-name"]));
        await wayneCatalog.CreateNamespaceAsync(new CreateNamespaceRequest(["shared-name"]));

        var snowyList = await snowyCatalog.ListNamespacesAsync();
        var wayneList = await wayneCatalog.ListNamespacesAsync();

        Assert.HasCount(1, snowyList.Namespaces);
        Assert.HasCount(1, wayneList.Namespaces);
    }

    [TestMethod]
    public void GetCatalog_SameTenant_ReturnsSameInstance()
    {
        var first = _resolver.GetCatalog("SnowyConesIceCream");
        var second = _resolver.GetCatalog("SnowyConesIceCream");

        Assert.AreSame(first, second);
    }

    [TestMethod]
    public void GetCatalog_DifferentTenants_ReturnsDifferentInstances()
    {
        var snowy = _resolver.GetCatalog("SnowyConesIceCream");
        var wayne = _resolver.GetCatalog("WayneEnterprises");

        Assert.AreNotSame(snowy, wayne);
    }
}
