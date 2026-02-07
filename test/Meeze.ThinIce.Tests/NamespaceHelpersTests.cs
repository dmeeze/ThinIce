using Meeze.ThinIce.Iceberg;

namespace Meeze.ThinIce.Tests;

[TestClass]
public class NamespaceHelpersTests
{
    public static IEnumerable<object[]> NamespaceCases =>
    [
        ["chill", new[] { "chill" }],
        ["ice\u001Fto\u001Fmeet\u001Fyou", new[] { "ice", "to", "meet", "you" }],
        ["lets\u001Fkick\u001Fsome\u001Fice", new[] { "lets", "kick", "some", "ice" }],
        ["allow\u001Fme\u001Fto\u001Fbreak\u001Fthe\u001Fice", new[] { "allow", "me", "to", "break", "the", "ice" }],
        ["cool\u001Fparty", new[] { "cool", "party" }],
        ["the\u001Ficeman\u001Fcometh", new[] { "the", "iceman", "cometh" }],
    ];

    [TestMethod]
    [DynamicData(nameof(NamespaceCases))]
    public void Parse_SplitsOnUnitSeparator(string encoded, string[] expected)
    {
        CollectionAssert.AreEqual(expected, NamespaceHelpers.Parse(encoded));
    }

    [TestMethod]
    [DynamicData(nameof(NamespaceCases))]
    public void Encode_JoinsWithUnitSeparator(string encoded, string[] levels)
    {
        Assert.AreEqual(encoded, NamespaceHelpers.Encode(levels));
    }

    [TestMethod]
    [DynamicData(nameof(NamespaceCases))]
    public void RoundTrip_ParseEncode(string encoded, string[] levels)
    {
        CollectionAssert.AreEqual(levels, NamespaceHelpers.Parse(NamespaceHelpers.Encode(levels)));
        Assert.AreEqual(encoded, NamespaceHelpers.Encode(NamespaceHelpers.Parse(encoded)));
    }
}
