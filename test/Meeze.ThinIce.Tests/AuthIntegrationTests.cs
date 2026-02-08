using System.Net;
using System.Net.Http.Headers;
using Meeze.ThinIce.Iceberg.Models;

namespace Meeze.ThinIce.Tests;

[TestClass]
public class AuthIntegrationTests
{
    private ThinIceWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;

    [TestInitialize]
    public void Setup()
    {
        _factory = new ThinIceWebApplicationFactory();
        _client = _factory.CreateClient();
    }

    [TestCleanup]
    public void Cleanup()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    [TestMethod]
    public async Task Config_NoAuth_Returns200()
    {
        var response = await _client.GetAsync("/v1/config");

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var config = await response.Content.ReadJsonAsync<CatalogConfig>();
        Assert.AreEqual("v1", config.Defaults!["prefix"]);
    }

    [TestMethod]
    public async Task ProtectedEndpoint_NoAuth_Returns401()
    {
        var response = await _client.GetAsync("/v1/namespaces");

        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task ProtectedEndpoint_BadToken_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "melted-token");

        var response = await _client.GetAsync("/v1/namespaces");

        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task ProtectedEndpoint_ValidToken_Returns200()
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "freeze-ray-token-001");

        var response = await _client.GetAsync("/v1/namespaces");

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }
}
