using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Meeze.ThinIce.Iceberg.Models;

namespace Meeze.ThinIce.Tests;

[TestClass]
public class TenantIsolationIntegrationTests
{
    private ThinIceWebApplicationFactory _factory = null!;
    private HttpClient _snowyClient = null!;
    private HttpClient _wayneClient = null!;

    [TestInitialize]
    public void Setup()
    {
        _factory = new ThinIceWebApplicationFactory();

        _snowyClient = _factory.CreateClient();
        _snowyClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "freeze-ray-token-001");

        _wayneClient = _factory.CreateClient();
        _wayneClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "ice-age-token-002");
    }

    [TestCleanup]
    public void Cleanup()
    {
        _snowyClient.Dispose();
        _wayneClient.Dispose();
        _factory.Dispose();
    }

    [TestMethod]
    public async Task DifferentTenants_NamespacesIsolated()
    {
        // SnowyConesIceCream creates a namespace
        await _snowyClient.PostAsJsonAsync("/v1/namespaces",
            new CreateNamespaceRequest(["snowy_secret"]));

        // WayneEnterprises should not see it
        var wayneList = await _wayneClient.GetAsync("/v1/namespaces");
        Assert.AreEqual(HttpStatusCode.OK, wayneList.StatusCode);
        var wayneNs = await wayneList.Content.ReadJsonAsync<ListNamespacesResponse>();
        Assert.IsEmpty(wayneNs.Namespaces);

        // SnowyConesIceCream should see it
        var snowyList = await _snowyClient.GetAsync("/v1/namespaces");
        var snowyNs = await snowyList.Content.ReadJsonAsync<ListNamespacesResponse>();
        Assert.HasCount(1, snowyNs.Namespaces);
    }

    [TestMethod]
    public async Task DifferentTenants_DataIsolated()
    {
        var content = new ByteArrayContent("mr-freeze-secret-formula"u8.ToArray());
        content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

        // SnowyConesIceCream writes data
        var putResponse = await _snowyClient.PutAsync("/v1/data/vault/formula.bin", content);
        Assert.AreEqual(HttpStatusCode.NoContent, putResponse.StatusCode);

        // WayneEnterprises cannot read it
        var wayneGet = await _wayneClient.GetAsync("/v1/data/vault/formula.bin");
        Assert.AreEqual(HttpStatusCode.NotFound, wayneGet.StatusCode);

        // SnowyConesIceCream can read it
        var snowyGet = await _snowyClient.GetAsync("/v1/data/vault/formula.bin");
        Assert.AreEqual(HttpStatusCode.OK, snowyGet.StatusCode);
    }
}
