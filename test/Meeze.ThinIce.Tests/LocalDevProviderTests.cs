using Meeze.ThinIce.Iceberg;
using Meeze.ThinIce.Iceberg.LocalDev;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using LocalDevOptions = Meeze.ThinIce.Iceberg.LocalDev.Options;

namespace Meeze.ThinIce.Tests;

[TestClass]
public class LocalDevProviderTests
{
    private Mock<IOptions<LocalDevOptions>> _mockOptions = null!;
    private Mock<ILoggerFactory> _mockLoggerFactory = null!;
    private Mock<ILogger<Catalog>> _mockCatalogLogger = null!;
    private Mock<ILogger<Storage>> _mockStorageLogger = null!;
    private string _tempDir = null!;

    [TestInitialize]
    public void Setup()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"ThinIce_Test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);

        _mockOptions = new Mock<IOptions<LocalDevOptions>>();
        _mockOptions.Setup(o => o.Value).Returns(new LocalDevOptions
        {
            BasePath = _tempDir,
            Tokens =
            [
                "freeze-ray-token-001:SnowyConesIceCream:mrfreeze@example.com",
                "ice-age-token-002:WayneEnterprises:batman@example.org"
            ]
        });

        _mockCatalogLogger = new Mock<ILogger<Catalog>>();
        _mockStorageLogger = new Mock<ILogger<Storage>>();

        _mockLoggerFactory = new Mock<ILoggerFactory>();
        _mockLoggerFactory.Setup(f => f.CreateLogger(It.IsAny<string>()))
            .Returns((string categoryName) => categoryName.Contains("Catalog")
                ? _mockCatalogLogger.Object
                : _mockStorageLogger.Object);
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    [TestMethod]
    [DataRow("SnowyConesIceCream")]
    [DataRow("WayneEnterprises")]
    [DataRow("FrozenTundra")]
    [DataRow("IcebergAlliance")]
    public void CanHandle_AnyTenant_ReturnsTrue(string tenant)
    {
        var provider = new Provider(_mockOptions.Object, _mockLoggerFactory.Object);

        var result = provider.CanHandle(tenant);

        Assert.IsTrue(result);
    }

    [TestMethod]
    public void GetCatalog_ValidTenant_ReturnsValidCatalogInstance()
    {
        var provider = new Provider(_mockOptions.Object, _mockLoggerFactory.Object);

        var catalog = provider.GetCatalog("SnowyConesIceCream");

        Assert.IsNotNull(catalog);
        Assert.IsInstanceOfType<Catalog>(catalog);
    }

    [TestMethod]
    public void GetCatalog_SameTenant_ReturnsSameInstance()
    {
        var provider = new Provider(_mockOptions.Object, _mockLoggerFactory.Object);

        var catalog1 = provider.GetCatalog("SnowyConesIceCream");
        var catalog2 = provider.GetCatalog("SnowyConesIceCream");

        Assert.AreSame(catalog1, catalog2);
    }

    [TestMethod]
    public void GetCatalog_DifferentTenants_ReturnsDifferentInstances()
    {
        var provider = new Provider(_mockOptions.Object, _mockLoggerFactory.Object);

        var catalog1 = provider.GetCatalog("SnowyConesIceCream");
        var catalog2 = provider.GetCatalog("WayneEnterprises");

        Assert.IsNotNull(catalog1);
        Assert.IsNotNull(catalog2);
        Assert.AreNotSame(catalog1, catalog2);
    }

    [TestMethod]
    public void GetStorage_ValidTenant_ReturnsValidStorageInstance()
    {
        var provider = new Provider(_mockOptions.Object, _mockLoggerFactory.Object);

        var storage = provider.GetStorage("SnowyConesIceCream");

        Assert.IsNotNull(storage);
        Assert.IsInstanceOfType<Storage>(storage);
    }

    [TestMethod]
    public void GetStorage_SameTenant_ReturnsSameInstance()
    {
        var provider = new Provider(_mockOptions.Object, _mockLoggerFactory.Object);

        var storage1 = provider.GetStorage("SnowyConesIceCream");
        var storage2 = provider.GetStorage("SnowyConesIceCream");

        Assert.AreSame(storage1, storage2);
    }

    [TestMethod]
    public void GetStorage_DifferentTenants_ReturnsDifferentInstances()
    {
        var provider = new Provider(_mockOptions.Object, _mockLoggerFactory.Object);

        var storage1 = provider.GetStorage("SnowyConesIceCream");
        var storage2 = provider.GetStorage("WayneEnterprises");

        Assert.IsNotNull(storage1);
        Assert.IsNotNull(storage2);
        Assert.AreNotSame(storage1, storage2);
    }

    [TestMethod]
    public void GetCatalogAndGetStorage_BothReturnValidInstances()
    {
        var provider = new Provider(_mockOptions.Object, _mockLoggerFactory.Object);

        var catalog = provider.GetCatalog("SnowyConesIceCream");
        var storage = provider.GetStorage("SnowyConesIceCream");

        Assert.IsNotNull(catalog);
        Assert.IsNotNull(storage);
        Assert.IsInstanceOfType<Catalog>(catalog);
        Assert.IsInstanceOfType<Storage>(storage);
    }

    [TestMethod]
    public void GetCatalog_CreatesLoggerWithCorrectType()
    {
        var provider = new Provider(_mockOptions.Object, _mockLoggerFactory.Object);

        _ = provider.GetCatalog("SnowyConesIceCream");

        _mockLoggerFactory.Verify(
            f => f.CreateLogger(It.Is<string>(s => s.Contains("Catalog"))),
            Times.AtLeastOnce);
    }

    [TestMethod]
    public void GetStorage_CreatesLoggerWithCorrectType()
    {
        var provider = new Provider(_mockOptions.Object, _mockLoggerFactory.Object);

        _ = provider.GetStorage("SnowyConesIceCream");

        _mockLoggerFactory.Verify(
            f => f.CreateLogger(It.Is<string>(s => s.Contains("Storage"))),
            Times.AtLeastOnce);
    }

    [TestMethod]
    public void GetCatalog_MultipleTenants_CreatesMultipleCatalogs()
    {
        var provider = new Provider(_mockOptions.Object, _mockLoggerFactory.Object);

        var catalog1 = provider.GetCatalog("SnowyConesIceCream");
        var catalog2 = provider.GetCatalog("WayneEnterprises");
        var catalog3 = provider.GetCatalog("FrozenTundra");

        Assert.IsNotNull(catalog1);
        Assert.IsNotNull(catalog2);
        Assert.IsNotNull(catalog3);
        Assert.AreNotSame(catalog1, catalog2);
        Assert.AreNotSame(catalog2, catalog3);
        Assert.AreNotSame(catalog1, catalog3);
    }

    [TestMethod]
    public void GetStorage_MultipleTenants_CreatesMultipleStorages()
    {
        var provider = new Provider(_mockOptions.Object, _mockLoggerFactory.Object);

        var storage1 = provider.GetStorage("SnowyConesIceCream");
        var storage2 = provider.GetStorage("WayneEnterprises");
        var storage3 = provider.GetStorage("FrozenTundra");

        Assert.IsNotNull(storage1);
        Assert.IsNotNull(storage2);
        Assert.IsNotNull(storage3);
        Assert.AreNotSame(storage1, storage2);
        Assert.AreNotSame(storage2, storage3);
        Assert.AreNotSame(storage1, storage3);
    }

    [TestMethod]
    public void GetCatalog_ConcurrentCalls_ReturnsSameInstance()
    {
        var provider = new Provider(_mockOptions.Object, _mockLoggerFactory.Object);
        var catalogs = new ICatalog[10];

        Parallel.For(0, 10, i =>
        {
            catalogs[i] = provider.GetCatalog("SnowyConesIceCream");
        });

        for (int i = 1; i < catalogs.Length; i++)
        {
            Assert.AreSame(catalogs[0], catalogs[i]);
        }
    }

    [TestMethod]
    public void GetStorage_ConcurrentCalls_ReturnsSameInstance()
    {
        var provider = new Provider(_mockOptions.Object, _mockLoggerFactory.Object);
        var storages = new IStorage[10];

        Parallel.For(0, 10, i =>
        {
            storages[i] = provider.GetStorage("SnowyConesIceCream");
        });

        for (int i = 1; i < storages.Length; i++)
        {
            Assert.AreSame(storages[0], storages[i]);
        }
    }
}
