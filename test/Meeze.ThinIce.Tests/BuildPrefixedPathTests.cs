using Meeze.ThinIce.App.Endpoints;

namespace Meeze.ThinIce.Tests;

[TestClass]
public class BuildPrefixedPathTests
{
    [TestMethod]
    [DataRow(null, "/namespaces", "/v1/namespaces")]
    [DataRow("", "/namespaces", "/v1/namespaces")]
    public void NullOrEmpty_NoPrefix(string? prefix, string path, string expected)
    {
        var result = EndpointRouteBuilderExtensions.BuildPrefixedPath(prefix, path);
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    [DataRow("cool/party", "/namespaces", "/v1/cool/party/namespaces")]
    [DataRow("ice/to/meet/you", "/namespaces/{ns}/tables", "/v1/ice/to/meet/you/namespaces/{ns}/tables")]
    [DataRow("freeze/coming", "/data", "/v1/freeze/coming/data")]
    public void MultiSegmentPrefix_Inserted(string prefix, string path, string expected)
    {
        var result = EndpointRouteBuilderExtensions.BuildPrefixedPath(prefix, path);
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    [DataRow("//cool/", "/namespaces", "/v1/cool/namespaces")]
    [DataRow("/chill/", "/data", "/v1/chill/data")]
    [DataRow("///ice/to/meet/you///", "/namespaces/{ns}/tables", "/v1/ice/to/meet/you/namespaces/{ns}/tables")]
    public void ExtraSlashes_Trimmed(string prefix, string path, string expected)
    {
        var result = EndpointRouteBuilderExtensions.BuildPrefixedPath(prefix, path);
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    [DataRow("cool", "/namespaces", "/v1/cool/namespaces")]
    [DataRow("chill", "/namespaces/{ns}/tables", "/v1/chill/namespaces/{ns}/tables")]
    public void BarePrefix_NoSlashes(string prefix, string path, string expected)
    {
        var result = EndpointRouteBuilderExtensions.BuildPrefixedPath(prefix, path);
        Assert.AreEqual(expected, result);
    }
}
