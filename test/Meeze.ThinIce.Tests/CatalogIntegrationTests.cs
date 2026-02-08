using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Meeze.ThinIce.Iceberg.Models;

namespace Meeze.ThinIce.Tests;

[TestClass]
public class CatalogIntegrationTests
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
    public async Task ListNamespaces_EmptyInitially()
    {
        var response = await _client.GetAsync("/v1/namespaces");

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadJsonAsync<ListNamespacesResponse>();
        Assert.IsEmpty(result.Namespaces);
    }

    [TestMethod]
    public async Task Namespace_CreateAndLoad()
    {
        var createRequest = new CreateNamespaceRequest(["frozen", "desserts"],
            new Dictionary<string, string> { ["flavor"] = "vanilla" });

        var createResponse = await _client.PostAsJsonAsync("/v1/namespaces", createRequest);
        Assert.AreEqual(HttpStatusCode.OK, createResponse.StatusCode);

        var created = await createResponse.Content.ReadJsonAsync<NamespaceDetail>();
        CollectionAssert.AreEqual(new[] { "frozen", "desserts" }, created.Namespace);
        Assert.AreEqual("vanilla", created.Properties!["flavor"]);

        // Load the namespace back (multi-level uses \u001F separator, URL-encoded as %1F)
        var loadResponse = await _client.GetAsync("/v1/namespaces/frozen%1Fdesserts");
        Assert.AreEqual(HttpStatusCode.OK, loadResponse.StatusCode);

        var loaded = await loadResponse.Content.ReadJsonAsync<NamespaceDetail>();
        CollectionAssert.AreEqual(new[] { "frozen", "desserts" }, loaded.Namespace);
        Assert.AreEqual("vanilla", loaded.Properties!["flavor"]);
    }

    [TestMethod]
    public async Task Namespace_CreateDuplicate_Returns409()
    {
        var request = new CreateNamespaceRequest(["iceberg"]);
        await _client.PostAsJsonAsync("/v1/namespaces", request);

        var duplicate = await _client.PostAsJsonAsync("/v1/namespaces", request);
        Assert.AreEqual(HttpStatusCode.Conflict, duplicate.StatusCode);
    }

    [TestMethod]
    public async Task Namespace_LoadNotFound_Returns404()
    {
        var response = await _client.GetAsync("/v1/namespaces/does.not.exist");
        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    [TestMethod]
    public async Task Namespace_DropAndVerify()
    {
        var request = new CreateNamespaceRequest(["meltable"]);
        await _client.PostAsJsonAsync("/v1/namespaces", request);

        var dropResponse = await _client.DeleteAsync("/v1/namespaces/meltable");
        Assert.AreEqual(HttpStatusCode.NoContent, dropResponse.StatusCode);

        var loadResponse = await _client.GetAsync("/v1/namespaces/meltable");
        Assert.AreEqual(HttpStatusCode.NotFound, loadResponse.StatusCode);
    }

    [TestMethod]
    public async Task Namespace_DropNotFound_Returns404()
    {
        var response = await _client.DeleteAsync("/v1/namespaces/phantom");
        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    [TestMethod]
    public async Task Table_FullCrudCycle()
    {
        // Create namespace first
        await _client.PostAsJsonAsync("/v1/namespaces", new CreateNamespaceRequest(["glacial"]));

        // Create table
        var schema = new Schema(0, [new SchemaField(1, "id", true, "long"), new SchemaField(2, "name", false, "string")]);
        var createRequest = new CreateTableRequest("icicles", schema);

        var createResponse = await _client.PostAsJsonAsync("/v1/namespaces/glacial/tables", createRequest);
        Assert.AreEqual(HttpStatusCode.OK, createResponse.StatusCode);

        var created = await createResponse.Content.ReadJsonAsync<LoadTableResponse>();
        Assert.AreEqual(2, created.Metadata.FormatVersion);
        Assert.HasCount(2, created.Metadata.Schemas[0].Fields);

        // List tables
        var listResponse = await _client.GetAsync("/v1/namespaces/glacial/tables");
        Assert.AreEqual(HttpStatusCode.OK, listResponse.StatusCode);

        var list = await listResponse.Content.ReadJsonAsync<ListTablesResponse>();
        Assert.HasCount(1, list.Identifiers);
        Assert.AreEqual("icicles", list.Identifiers[0].Name);

        // Load table
        var loadResponse = await _client.GetAsync("/v1/namespaces/glacial/tables/icicles");
        Assert.AreEqual(HttpStatusCode.OK, loadResponse.StatusCode);

        var loaded = await loadResponse.Content.ReadJsonAsync<LoadTableResponse>();
        Assert.AreEqual(created.Metadata.TableUuid, loaded.Metadata.TableUuid);

        // Drop table
        var dropResponse = await _client.DeleteAsync("/v1/namespaces/glacial/tables/icicles");
        Assert.AreEqual(HttpStatusCode.NoContent, dropResponse.StatusCode);

        // Verify gone
        var goneResponse = await _client.GetAsync("/v1/namespaces/glacial/tables/icicles");
        Assert.AreEqual(HttpStatusCode.NotFound, goneResponse.StatusCode);
    }

    [TestMethod]
    public async Task Table_CreateDuplicate_Returns409()
    {
        await _client.PostAsJsonAsync("/v1/namespaces", new CreateNamespaceRequest(["dupes"]));

        var schema = new Schema(0, [new SchemaField(1, "id", true, "long")]);
        var request = new CreateTableRequest("same_table", schema);
        await _client.PostAsJsonAsync("/v1/namespaces/dupes/tables", request);

        var duplicate = await _client.PostAsJsonAsync("/v1/namespaces/dupes/tables", request);
        Assert.AreEqual(HttpStatusCode.Conflict, duplicate.StatusCode);
    }

    [TestMethod]
    public async Task Table_LoadNotFound_Returns404()
    {
        await _client.PostAsJsonAsync("/v1/namespaces", new CreateNamespaceRequest(["empty_ns"]));

        var response = await _client.GetAsync("/v1/namespaces/empty_ns/tables/ghost_table");
        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    [TestMethod]
    public async Task Table_ListInNonexistentNamespace_Returns404()
    {
        var response = await _client.GetAsync("/v1/namespaces/no_such_ns/tables");
        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
    }
}
