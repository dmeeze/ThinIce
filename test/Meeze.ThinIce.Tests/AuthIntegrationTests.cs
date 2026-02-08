using System.Net;
using System.Net.Http.Headers;
using Meeze.ThinIce.Auth.Models;
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
    public async Task TokenExchange_ValidCredentials_ReturnsAccessToken()
    {
        var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials",
            ["client_id"] = "freeze-ray-token-001",
            ["client_secret"] = ""
        });

        var response = await _client.PostAsync("/v1/oauth/tokens", form);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var token = await response.Content.ReadJsonAsync<OAuthTokenResponse>();
        Assert.AreEqual("freeze-ray-token-001", token.AccessToken);
        Assert.AreEqual("bearer", token.TokenType);
    }

    [TestMethod]
    public async Task TokenExchange_InvalidCredentials_Returns401()
    {
        var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials",
            ["client_id"] = "melted-token",
            ["client_secret"] = ""
        });

        var response = await _client.PostAsync("/v1/oauth/tokens", form);

        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task TokenExchange_MissingGrantType_Returns400()
    {
        var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["client_id"] = "freeze-ray-token-001"
        });

        var response = await _client.PostAsync("/v1/oauth/tokens", form);

        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
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
    public async Task AuthFlow_ExchangeToken_ThenAccessProtected()
    {
        // Exchange credentials for an access token
        var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials",
            ["client_id"] = "freeze-ray-token-001",
            ["client_secret"] = ""
        });
        var exchangeResponse = await _client.PostAsync("/v1/oauth/tokens", form);
        Assert.AreEqual(HttpStatusCode.OK, exchangeResponse.StatusCode);

        var token = await exchangeResponse.Content.ReadJsonAsync<OAuthTokenResponse>();

        // Use the access token to hit a protected endpoint
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);

        var namespacesResponse = await _client.GetAsync("/v1/namespaces");
        Assert.AreEqual(HttpStatusCode.OK, namespacesResponse.StatusCode);
    }
}
