using System.Net;
using System.Net.Http.Headers;
using System.Text;

namespace Meeze.ThinIce.Tests;

[TestClass]
public class DataIntegrationTests
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
    public async Task Data_WriteAndRead_RoundTrip()
    {
        var content = "frozen-payload-bytes-001"u8.ToArray();
        var putContent = new ByteArrayContent(content);
        putContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

        var putResponse = await _client.PutAsync("/v1/data/warehouse/ice-cubes.parquet", putContent);
        Assert.AreEqual(HttpStatusCode.NoContent, putResponse.StatusCode);

        var getResponse = await _client.GetAsync("/v1/data/warehouse/ice-cubes.parquet");
        Assert.AreEqual(HttpStatusCode.OK, getResponse.StatusCode);

        var readBytes = await getResponse.Content.ReadAsByteArrayAsync();
        CollectionAssert.AreEqual(content, readBytes);
    }

    [TestMethod]
    public async Task Data_ReadNotFound_Returns404()
    {
        var response = await _client.GetAsync("/v1/data/warehouse/melted.parquet");
        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
    }
}
