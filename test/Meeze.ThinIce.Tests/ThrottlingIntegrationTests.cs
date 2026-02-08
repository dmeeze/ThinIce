using System.Net;
using System.Net.Http.Headers;

namespace Meeze.ThinIce.Tests;

[TestClass]
public class ThrottlingIntegrationTests
{
    [TestMethod]
    public async Task Config_ExceedsLimit_Returns429()
    {
        using var factory = new ThinIceWebApplicationFactory(throttling: new Dictionary<string, string?>
        {
            ["Throttling:ConfigPermitsPerMinute"] = "3"
        });
        using var client = factory.CreateClient();

        for (var i = 0; i < 3; i++)
        {
            var ok = await client.GetAsync("/v1/config");
            Assert.AreEqual(HttpStatusCode.OK, ok.StatusCode, $"Request {i + 1} should succeed");
        }

        var rejected = await client.GetAsync("/v1/config");
        Assert.AreEqual(HttpStatusCode.TooManyRequests, rejected.StatusCode);
    }

    [TestMethod]
    public async Task Authenticated_ExceedsLimit_Returns429()
    {
        using var factory = new ThinIceWebApplicationFactory(throttling: new Dictionary<string, string?>
        {
            ["Throttling:AuthenticatedPermitsPerMinute"] = "3"
        });
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "freeze-ray-token-001");

        for (var i = 0; i < 3; i++)
        {
            var ok = await client.GetAsync("/v1/namespaces");
            Assert.AreEqual(HttpStatusCode.OK, ok.StatusCode, $"Request {i + 1} should succeed");
        }

        var rejected = await client.GetAsync("/v1/namespaces");
        Assert.AreEqual(HttpStatusCode.TooManyRequests, rejected.StatusCode);
    }

    [TestMethod]
    public async Task Authenticated_LimitIsSharedAcrossEndpoints()
    {
        using var factory = new ThinIceWebApplicationFactory(throttling: new Dictionary<string, string?>
        {
            ["Throttling:AuthenticatedPermitsPerMinute"] = "4"
        });
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "freeze-ray-token-001");

        // Use 2 permits on namespaces
        for (var i = 0; i < 2; i++)
        {
            var ok = await client.GetAsync("/v1/namespaces");
            Assert.AreEqual(HttpStatusCode.OK, ok.StatusCode);
        }

        // Use 2 permits on data (will 404 but should not be rate-limited)
        for (var i = 0; i < 2; i++)
        {
            var ok = await client.GetAsync("/v1/data/nonexistent");
            Assert.AreEqual(HttpStatusCode.NotFound, ok.StatusCode);
        }

        // 5th request across any authenticated endpoint should be rejected
        var rejected = await client.GetAsync("/v1/namespaces");
        Assert.AreEqual(HttpStatusCode.TooManyRequests, rejected.StatusCode);
    }

    [TestMethod]
    public async Task Config_WithinLimit_Succeeds()
    {
        using var factory = new ThinIceWebApplicationFactory(throttling: new Dictionary<string, string?>
        {
            ["Throttling:ConfigPermitsPerMinute"] = "5"
        });
        using var client = factory.CreateClient();

        for (var i = 0; i < 5; i++)
        {
            var response = await client.GetAsync("/v1/config");
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, $"Request {i + 1} should succeed");
        }
    }
}
