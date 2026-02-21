using Meeze.ThinIce.Iceberg;
using Microsoft.Extensions.Logging;
using Moq;

namespace Meeze.ThinIce.Tests;

[TestClass]
public class IcebergRouterTests
{
    private Mock<ILogger<DefaultIcebergRouter>> _mockLogger = null!;

    [TestInitialize]
    public void Setup()
    {
        _mockLogger = new Mock<ILogger<DefaultIcebergRouter>>();
    }

    [TestMethod]
    public void GetProvider_NoProviders_ThrowsException()
    {
        var providers = Array.Empty<IIcebergProvider>();
        var router = new DefaultIcebergRouter(providers, _mockLogger.Object);

        var exception = Assert.Throws<InvalidOperationException>(() =>
            router.GetProvider("SnowyConesIceCream"));

        Assert.IsTrue(exception.Message.Contains("No provider found for tenant"));
        Assert.IsTrue(exception.Message.Contains("SnowyConesIceCream"));
    }

    [TestMethod]
    public void GetProvider_SingleProvider_CanHandle_ReturnsProvider()
    {
        var mockProvider = new Mock<IIcebergProvider>();
        mockProvider.Setup(p => p.CanHandle("SnowyConesIceCream")).Returns(true);

        var router = new DefaultIcebergRouter([mockProvider.Object], _mockLogger.Object);

        var result = router.GetProvider("SnowyConesIceCream");

        Assert.AreSame(mockProvider.Object, result);
        mockProvider.Verify(p => p.CanHandle("SnowyConesIceCream"), Times.Once);
    }

    [TestMethod]
    public void GetProvider_SingleProvider_CannotHandle_ThrowsException()
    {
        var mockProvider = new Mock<IIcebergProvider>();
        mockProvider.Setup(p => p.CanHandle("SnowyConesIceCream")).Returns(false);

        var router = new DefaultIcebergRouter([mockProvider.Object], _mockLogger.Object);

        var exception = Assert.Throws<InvalidOperationException>(() =>
            router.GetProvider("SnowyConesIceCream"));

        Assert.IsTrue(exception.Message.Contains("No provider found for tenant"));
        Assert.IsTrue(exception.Message.Contains("SnowyConesIceCream"));
        mockProvider.Verify(p => p.CanHandle("SnowyConesIceCream"), Times.Once);
    }

    [TestMethod]
    public void GetProvider_MultipleProviders_ReturnsFirstMatchingProvider()
    {
        var mockProvider1 = new Mock<IIcebergProvider>();
        mockProvider1.Setup(p => p.CanHandle("SnowyConesIceCream")).Returns(false);

        var mockProvider2 = new Mock<IIcebergProvider>();
        mockProvider2.Setup(p => p.CanHandle("SnowyConesIceCream")).Returns(true);

        var mockProvider3 = new Mock<IIcebergProvider>();
        mockProvider3.Setup(p => p.CanHandle("SnowyConesIceCream")).Returns(true);

        var router = new DefaultIcebergRouter(
            [mockProvider1.Object, mockProvider2.Object, mockProvider3.Object],
            _mockLogger.Object);

        var result = router.GetProvider("SnowyConesIceCream");

        Assert.AreSame(mockProvider2.Object, result);
        mockProvider1.Verify(p => p.CanHandle("SnowyConesIceCream"), Times.Once);
        mockProvider2.Verify(p => p.CanHandle("SnowyConesIceCream"), Times.Once);
        mockProvider3.Verify(p => p.CanHandle("SnowyConesIceCream"), Times.Never);
    }

    [TestMethod]
    public void GetProvider_MultipleProviders_ChainOfResponsibility()
    {
        var mockProvider1 = new Mock<IIcebergProvider>();
        mockProvider1.Setup(p => p.CanHandle(It.IsAny<string>())).Returns(false);

        var mockProvider2 = new Mock<IIcebergProvider>();
        mockProvider2.Setup(p => p.CanHandle(It.IsAny<string>())).Returns(false);

        var mockProvider3 = new Mock<IIcebergProvider>();
        mockProvider3.Setup(p => p.CanHandle("FrozenTundra")).Returns(true);

        var router = new DefaultIcebergRouter(
            [mockProvider1.Object, mockProvider2.Object, mockProvider3.Object],
            _mockLogger.Object);

        var result = router.GetProvider("FrozenTundra");

        Assert.AreSame(mockProvider3.Object, result);
        mockProvider1.Verify(p => p.CanHandle("FrozenTundra"), Times.Once);
        mockProvider2.Verify(p => p.CanHandle("FrozenTundra"), Times.Once);
        mockProvider3.Verify(p => p.CanHandle("FrozenTundra"), Times.Once);
    }

    [TestMethod]
    public void GetProvider_MultipleProviders_NoneCanHandle_ThrowsException()
    {
        var mockProvider1 = new Mock<IIcebergProvider>();
        mockProvider1.Setup(p => p.CanHandle("IcebergAlliance")).Returns(false);

        var mockProvider2 = new Mock<IIcebergProvider>();
        mockProvider2.Setup(p => p.CanHandle("IcebergAlliance")).Returns(false);

        var router = new DefaultIcebergRouter(
            [mockProvider1.Object, mockProvider2.Object],
            _mockLogger.Object);

        var exception = Assert.Throws<InvalidOperationException>(() =>
            router.GetProvider("IcebergAlliance"));

        Assert.IsTrue(exception.Message.Contains("No provider found for tenant"));
        Assert.IsTrue(exception.Message.Contains("IcebergAlliance"));
        mockProvider1.Verify(p => p.CanHandle("IcebergAlliance"), Times.Once);
        mockProvider2.Verify(p => p.CanHandle("IcebergAlliance"), Times.Once);
    }

    [TestMethod]
    [DataRow("SnowyConesIceCream")]
    [DataRow("WayneEnterprises")]
    [DataRow("FrozenTundra")]
    [DataRow("IcebergAlliance")]
    public void GetProvider_DifferentTenants_CallsCanHandleWithCorrectTenant(string tenant)
    {
        var mockProvider = new Mock<IIcebergProvider>();
        mockProvider.Setup(p => p.CanHandle(tenant)).Returns(true);

        var router = new DefaultIcebergRouter([mockProvider.Object], _mockLogger.Object);

        var result = router.GetProvider(tenant);

        Assert.AreSame(mockProvider.Object, result);
        mockProvider.Verify(p => p.CanHandle(tenant), Times.Once);
    }

    [TestMethod]
    public void GetProvider_SelectiveProviders_RoutesCorrectly()
    {
        var localDevProvider = new Mock<IIcebergProvider>();
        localDevProvider.Setup(p => p.CanHandle(It.IsAny<string>())).Returns(true);

        var s3Provider = new Mock<IIcebergProvider>();
        s3Provider.Setup(p => p.CanHandle("WayneEnterprises")).Returns(true);
        s3Provider.Setup(p => p.CanHandle(It.Is<string>(s => s != "WayneEnterprises"))).Returns(false);

        var router = new DefaultIcebergRouter(
            [s3Provider.Object, localDevProvider.Object],
            _mockLogger.Object);

        var resultWayne = router.GetProvider("WayneEnterprises");
        var resultSnowy = router.GetProvider("SnowyConesIceCream");

        Assert.AreSame(s3Provider.Object, resultWayne);
        Assert.AreSame(localDevProvider.Object, resultSnowy);
    }

    [TestMethod]
    public void GetProvider_ProviderCanHandleMultipleTenants_Works()
    {
        var mockProvider = new Mock<IIcebergProvider>();
        mockProvider.Setup(p => p.CanHandle(It.IsAny<string>())).Returns(true);

        var router = new DefaultIcebergRouter([mockProvider.Object], _mockLogger.Object);

        var result1 = router.GetProvider("SnowyConesIceCream");
        var result2 = router.GetProvider("WayneEnterprises");
        var result3 = router.GetProvider("FrozenTundra");

        Assert.AreSame(mockProvider.Object, result1);
        Assert.AreSame(mockProvider.Object, result2);
        Assert.AreSame(mockProvider.Object, result3);
    }

    [TestMethod]
    public void GetProvider_ProviderOrderMatters_ReturnsFirstMatch()
    {
        var highPriorityProvider = new Mock<IIcebergProvider>();
        highPriorityProvider.Setup(p => p.CanHandle("SnowyConesIceCream")).Returns(true);

        var lowPriorityProvider = new Mock<IIcebergProvider>();
        lowPriorityProvider.Setup(p => p.CanHandle("SnowyConesIceCream")).Returns(true);

        var router = new DefaultIcebergRouter(
            [highPriorityProvider.Object, lowPriorityProvider.Object],
            _mockLogger.Object);

        var result = router.GetProvider("SnowyConesIceCream");

        Assert.AreSame(highPriorityProvider.Object, result);
        lowPriorityProvider.Verify(p => p.CanHandle(It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public void GetProvider_ProviderThrowsOnCanHandle_PropagatesException()
    {
        var mockProvider = new Mock<IIcebergProvider>();
        mockProvider.Setup(p => p.CanHandle("SnowyConesIceCream"))
            .Throws(new InvalidOperationException("Provider initialization failed"));

        var router = new DefaultIcebergRouter([mockProvider.Object], _mockLogger.Object);

        var exception = Assert.Throws<InvalidOperationException>(() =>
            router.GetProvider("SnowyConesIceCream"));

        Assert.AreEqual("Provider initialization failed", exception.Message);
    }

    [TestMethod]
    public void GetProvider_MultipleCallsSameTenant_CallsCanHandleEachTime()
    {
        var mockProvider = new Mock<IIcebergProvider>();
        mockProvider.Setup(p => p.CanHandle("SnowyConesIceCream")).Returns(true);

        var router = new DefaultIcebergRouter([mockProvider.Object], _mockLogger.Object);

        _ = router.GetProvider("SnowyConesIceCream");
        _ = router.GetProvider("SnowyConesIceCream");
        _ = router.GetProvider("SnowyConesIceCream");

        mockProvider.Verify(p => p.CanHandle("SnowyConesIceCream"), Times.Exactly(3));
    }
}
