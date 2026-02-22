using Meeze.ThinIce;

namespace Meeze.ThinIce.Tests;

[TestClass]
public class TenantContextTests
{
    [TestMethod]
    public void IsAuthenticated_TrueWhenTenantSet()
    {
        var ctx = new TenantContext("SnowyConesIceCream", "freeze@example.com", "LocalDev");
        Assert.IsTrue(ctx.IsAuthenticated);
    }

    [TestMethod]
    public void Anonymous_IsNotAuthenticated()
    {
        Assert.IsFalse(TenantContext.Anonymous.IsAuthenticated);
    }

    [TestMethod]
    public void RecordEquality_SameValues()
    {
        var a = new TenantContext("SnowyConesIceCream", "freeze@example.com", "LocalDev");
        var b = new TenantContext("SnowyConesIceCream", "freeze@example.com", "LocalDev");
        Assert.AreEqual(a, b);
    }

    [TestMethod]
    public void RecordEquality_DifferentValues()
    {
        var a = new TenantContext("SnowyConesIceCream", "freeze@example.com", "LocalDev");
        var b = new TenantContext("WayneEnterprises", "batman@example.org", "LocalDev");
        Assert.AreNotEqual(a, b);
    }

    [TestMethod]
    [DataRow("SnowyConesIceCream", "freeze@example.com", "LocalDev")]
    [DataRow("WayneEnterprises", "batman@example.org", "S3")]
    public void Properties_PreservedFromConstructor(string tenant, string user, string key)
    {
        var ctx = new TenantContext(tenant, user, key);
        Assert.AreEqual(tenant, ctx.Tenant);
        Assert.AreEqual(user, ctx.User);
        Assert.AreEqual(key, ctx.ProviderKey);
    }
}
