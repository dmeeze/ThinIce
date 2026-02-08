using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Meeze.ThinIce.Iceberg.LocalDev;

namespace Meeze.ThinIce.Tests;

[TestClass]
public class LocalDevAuthProviderTests
{
    private static readonly LocalDevOptions TestOptions = new()
    {
        BasePath = Path.Combine(Path.GetTempPath(), "ThinIce_AuthProviderTest"),
        Tokens =
        [
            "freeze-ray-token-001:SnowyConesIceCream:mrfreeze@example.com",
            "ice-age-token-002:WayneEnterprises:batman@example.org"
        ]
    };

    private static LocalDevAuthProvider CreateProvider(LocalDevOptions? options = null)
    {
        var opts = Options.Create(options ?? TestOptions);
        return new LocalDevAuthProvider(opts, NullLogger<LocalDevAuthProvider>.Instance);
    }

    [TestMethod]
    public void ParseTokens_ValidEntries_ParsesCorrectly()
    {
        var result = LocalDevAuthProvider.ParseTokens(TestOptions.Tokens);

        Assert.HasCount(2, result);
        Assert.AreEqual(("SnowyConesIceCream", "mrfreeze@example.com"), result["freeze-ray-token-001"]);
        Assert.AreEqual(("WayneEnterprises", "batman@example.org"), result["ice-age-token-002"]);
    }

    [TestMethod]
    public void ParseTokens_MalformedEntry_Skipped()
    {
        var result = LocalDevAuthProvider.ParseTokens(["good-token:Tenant:user@example.com", "bad-entry-no-colons"]);

        Assert.HasCount(1, result);
        Assert.IsTrue(result.ContainsKey("good-token"));
    }

    [TestMethod]
    public void ParseTokens_EmptyList_ReturnsEmpty()
    {
        var result = LocalDevAuthProvider.ParseTokens([]);
        Assert.IsEmpty(result);
    }

    [TestMethod]
    public async Task ValidateTokenAsync_ValidToken_ReturnsTenantContext()
    {
        var provider = CreateProvider();

        var result = await provider.ValidateTokenAsync("freeze-ray-token-001");

        Assert.IsNotNull(result);
        Assert.AreEqual("SnowyConesIceCream", result.Tenant);
        Assert.AreEqual("mrfreeze@example.com", result.User);
        Assert.AreEqual("LocalDev", result.ProviderKey);
    }

    [TestMethod]
    public async Task ValidateTokenAsync_InvalidToken_ReturnsNull()
    {
        var provider = CreateProvider();

        var result = await provider.ValidateTokenAsync("totally-bogus-token");

        Assert.IsNull(result);
    }

    [TestMethod]
    public void ProviderKey_ReturnsLocalDev()
    {
        var provider = CreateProvider();
        Assert.AreEqual("LocalDev", provider.ProviderKey);
    }
}
