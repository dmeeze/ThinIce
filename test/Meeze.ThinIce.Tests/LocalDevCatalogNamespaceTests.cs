using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Meeze.ThinIce.Iceberg.LocalDev;
using Meeze.ThinIce.Iceberg.Models;

namespace Meeze.ThinIce.Tests;

[TestClass]
public class LocalDevCatalogNamespaceTests
{
    private string _tempDir = null!;
    private string _namespacesRoot = null!;
    private LocalDevCatalog _catalog = null!;

    [TestInitialize]
    public void Setup()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"ThinIce_Test_{Guid.NewGuid():N}");
        _namespacesRoot = Path.Combine(_tempDir, "namespaces");
        Directory.CreateDirectory(_tempDir);
        _catalog = new LocalDevCatalog(_namespacesRoot, NullLogger<LocalDevCatalog>.Instance);
    }

    [TestCleanup]
    public void Cleanup()
    {
        _catalog.Dispose();
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    [TestMethod]
    public async Task ListNamespaces_Empty_ReturnsEmptyList()
    {
        var result = await _catalog.ListNamespacesAsync();

        Assert.IsEmpty(result.Namespaces);
    }

    [TestMethod]
    public async Task CreateNamespace_NewNamespace_CreatesAndReturnsDetail()
    {
        var request = new CreateNamespaceRequest(["frozen", "desserts"], new Dictionary<string, string>
        {
            ["flavor"] = "vanilla-frost"
        });

        var result = await _catalog.CreateNamespaceAsync(request);

        Assert.HasCount(2, result.Namespace);
        Assert.AreEqual("frozen", result.Namespace[0]);
        Assert.AreEqual("desserts", result.Namespace[1]);
        Assert.IsNotNull(result.Properties);
        Assert.AreEqual("vanilla-frost", result.Properties["flavor"]);
    }

    [TestMethod]
    public async Task CreateNamespace_NoProperties_CreatesWithEmptyProperties()
    {
        var request = new CreateNamespaceRequest(["bare-ice"]);

        var result = await _catalog.CreateNamespaceAsync(request);

        Assert.IsNotNull(result.Properties);
        Assert.IsEmpty(result.Properties);
    }

    [TestMethod]
    public async Task CreateNamespace_AlreadyExists_ThrowsInvalidOperation()
    {
        var request = new CreateNamespaceRequest(["mr-freeze-lab"]);
        await _catalog.CreateNamespaceAsync(request);

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => _catalog.CreateNamespaceAsync(request));
    }

    [TestMethod]
    public async Task LoadNamespace_Exists_ReturnsDetail()
    {
        var request = new CreateNamespaceRequest(["ice-rink"], new Dictionary<string, string>
        {
            ["temperature"] = "-5C"
        });
        await _catalog.CreateNamespaceAsync(request);

        var loaded = await _catalog.LoadNamespaceAsync(["ice-rink"]);

        Assert.AreEqual("ice-rink", loaded.Namespace[0]);
        Assert.IsNotNull(loaded.Properties);
        Assert.AreEqual("-5C", loaded.Properties["temperature"]);
    }

    [TestMethod]
    public async Task LoadNamespace_NotFound_ThrowsDirectoryNotFound()
    {
        await Assert.ThrowsExactlyAsync<DirectoryNotFoundException>(
            () => _catalog.LoadNamespaceAsync(["ghost-rink"]));
    }

    [TestMethod]
    public async Task DropNamespace_Exists_RemovesDirectory()
    {
        var request = new CreateNamespaceRequest(["meltdown"]);
        await _catalog.CreateNamespaceAsync(request);

        await _catalog.DropNamespaceAsync(["meltdown"]);

        var list = await _catalog.ListNamespacesAsync();
        Assert.IsEmpty(list.Namespaces);
    }

    [TestMethod]
    public async Task DropNamespace_NotFound_ThrowsDirectoryNotFound()
    {
        await Assert.ThrowsExactlyAsync<DirectoryNotFoundException>(
            () => _catalog.DropNamespaceAsync(["never-frozen"]));
    }

    [TestMethod]
    public async Task FullCrudCycle_CreateListLoadDrop()
    {
        await _catalog.CreateNamespaceAsync(
            new CreateNamespaceRequest(["batcave", "freezer"]));
        await _catalog.CreateNamespaceAsync(
            new CreateNamespaceRequest(["arctic-lab"]));

        var list = await _catalog.ListNamespacesAsync();
        Assert.HasCount(2, list.Namespaces);

        var loaded = await _catalog.LoadNamespaceAsync(["arctic-lab"]);
        Assert.AreEqual("arctic-lab", loaded.Namespace[0]);

        await _catalog.DropNamespaceAsync(["arctic-lab"]);

        var afterDrop = await _catalog.ListNamespacesAsync();
        Assert.HasCount(1, afterDrop.Namespaces);
    }
}

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
