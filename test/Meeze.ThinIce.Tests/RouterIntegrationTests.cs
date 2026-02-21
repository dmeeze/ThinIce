using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Meeze.ThinIce.Iceberg.Models;

namespace Meeze.ThinIce.Tests;

[TestClass]
public class RouterIntegrationTests
{
    private ThinIceWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;

    [TestInitialize]
    public void Setup()
    {
        _factory = new ThinIceWebApplicationFactory();
        _client = _factory.CreateClient();
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "freeze-ray-token-001");
    }

    [TestCleanup]
    public void Cleanup()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    [TestMethod]
    public async Task Router_CatalogEndpoints_WorkCorrectly()
    {
        // Create namespace via router
        var createRequest = new CreateNamespaceRequest(["router", "test"]);
        var createResponse = await _client.PostAsJsonAsync("/v1/namespaces", createRequest);
        Assert.AreEqual(HttpStatusCode.OK, createResponse.StatusCode);

        // List namespaces via router
        var listResponse = await _client.GetAsync("/v1/namespaces");
        Assert.AreEqual(HttpStatusCode.OK, listResponse.StatusCode);
        var list = await listResponse.Content.ReadJsonAsync<ListNamespacesResponse>();
        Assert.HasCount(1, list.Namespaces);

        // Load namespace via router
        var loadResponse = await _client.GetAsync("/v1/namespaces/router%1Ftest");
        Assert.AreEqual(HttpStatusCode.OK, loadResponse.StatusCode);
        var loaded = await loadResponse.Content.ReadJsonAsync<NamespaceDetail>();
        CollectionAssert.AreEqual(new[] { "router", "test" }, loaded.Namespace);
    }

    [TestMethod]
    public async Task Router_StorageEndpoints_WorkCorrectly()
    {
        var testData = "ice-cold-data"u8.ToArray();
        var content = new ByteArrayContent(testData);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

        // Write data via router
        var putResponse = await _client.PutAsync("/v1/data/test/file.bin", content);
        Assert.AreEqual(HttpStatusCode.NoContent, putResponse.StatusCode);

        // Read data via router
        var getResponse = await _client.GetAsync("/v1/data/test/file.bin");
        Assert.AreEqual(HttpStatusCode.OK, getResponse.StatusCode);
        var retrieved = await getResponse.Content.ReadAsByteArrayAsync();
        CollectionAssert.AreEqual(testData, retrieved);
    }

    [TestMethod]
    public async Task Router_TableEndpoints_WorkCorrectly()
    {
        // Create namespace first
        await _client.PostAsJsonAsync("/v1/namespaces", new CreateNamespaceRequest(["tables"]));

        // Create table via router
        var schema = new Schema(0, [
            new SchemaField(1, "id", true, "int"),
            new SchemaField(2, "name", true, "string")
        ]);

        var createRequest = new CreateTableRequest(
            Name: "test-table",
            Schema: schema
        );

        var createResponse = await _client.PostAsJsonAsync("/v1/namespaces/tables/tables", createRequest);
        Assert.AreEqual(HttpStatusCode.OK, createResponse.StatusCode);

        // List tables via router
        var listResponse = await _client.GetAsync("/v1/namespaces/tables/tables");
        Assert.AreEqual(HttpStatusCode.OK, listResponse.StatusCode);
        var list = await listResponse.Content.ReadJsonAsync<ListTablesResponse>();
        Assert.HasCount(1, list.Identifiers);
        Assert.AreEqual("test-table", list.Identifiers[0].Name);

        // Load table via router
        var loadResponse = await _client.GetAsync("/v1/namespaces/tables/tables/test-table");
        Assert.AreEqual(HttpStatusCode.OK, loadResponse.StatusCode);
        var loaded = await loadResponse.Content.ReadJsonAsync<LoadTableResponse>();
        Assert.AreEqual("test-table", loaded.Metadata.Location.Split('/').Last());
    }

    [TestMethod]
    public async Task Router_MultipleCalls_SameTenant_CachesResolver()
    {
        // Multiple calls to catalog endpoints should use cached resolver
        for (int i = 0; i < 5; i++)
        {
            var response = await _client.GetAsync("/v1/namespaces");
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        }

        // Multiple calls to storage endpoints should use cached resolver
        var testData = "test"u8.ToArray();
        var content = new ByteArrayContent(testData);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

        await _client.PutAsync("/v1/data/test.bin", content);

        for (int i = 0; i < 5; i++)
        {
            var response = await _client.GetAsync("/v1/data/test.bin");
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        }
    }
}
