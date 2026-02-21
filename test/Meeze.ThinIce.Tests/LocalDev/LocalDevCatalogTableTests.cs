using Microsoft.Extensions.Logging.Abstractions;
using Meeze.ThinIce.Iceberg.Models;

namespace Meeze.ThinIce.Iceberg.LocalDev.Tests;

[TestClass]
public class CatalogTableTests
{
    private string _tempDir = null!;
    private string _namespacesRoot = null!;
    private Catalog _catalog = null!;

    private static readonly string[] TestNamespace = ["arctic-vault"];

    private static Schema SimpleSchema => new(0,
    [
        new SchemaField(1, "id", true, "long"),
        new SchemaField(2, "name", false, "string")
    ]);

    [TestInitialize]
    public void Setup()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"ThinIce_Test_{Guid.NewGuid():N}");
        _namespacesRoot = Path.Combine(_tempDir, "namespaces");
        Directory.CreateDirectory(_tempDir);
        _catalog = new Catalog(_namespacesRoot, NullLogger<Catalog>.Instance);
    }

    [TestCleanup]
    public void Cleanup()
    {
        _catalog.Dispose();
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    private async Task CreateTestNamespace(string[]? ns = null)
    {
        ns ??= TestNamespace;
        await _catalog.CreateNamespaceAsync(new CreateNamespaceRequest(ns));
    }

    [TestMethod]
    public async Task ListTables_Empty_ReturnsEmptyList()
    {
        await CreateTestNamespace();

        var result = await _catalog.ListTablesAsync(TestNamespace);

        Assert.IsEmpty(result.Identifiers);
    }

    [TestMethod]
    public async Task CreateTable_NewTable_CreatesAndReturnsMetadata()
    {
        await CreateTestNamespace();
        var request = new CreateTableRequest("freeze-ray-targets", SimpleSchema);

        var result = await _catalog.CreateTableAsync(TestNamespace, request);

        Assert.IsNotNull(result.Metadata);
        Assert.AreEqual(2, result.Metadata.FormatVersion);
        Assert.IsFalse(string.IsNullOrEmpty(result.Metadata.TableUuid));
        Assert.AreEqual(-1, result.Metadata.CurrentSnapshotId);
        Assert.AreEqual(0, result.Metadata.LastSequenceNumber);
        Assert.AreEqual(0, result.Metadata.CurrentSchemaId);
        Assert.HasCount(1, result.Metadata.Schemas);
        Assert.HasCount(2, result.Metadata.Schemas[0].Fields);
        Assert.AreEqual("freeze-ray-targets", Path.GetFileName(result.Metadata.Location));
    }

    [TestMethod]
    public async Task CreateTable_WithPartitionSpec_IncludesInMetadata()
    {
        await CreateTestNamespace();
        var partitionSpec = new PartitionSpec(0, [new PartitionField(1, 1000, "id_bucket", "bucket[16]")]);
        var request = new CreateTableRequest("ice-crystal-inventory", SimpleSchema, PartitionSpec: partitionSpec);

        var result = await _catalog.CreateTableAsync(TestNamespace, request);

        Assert.HasCount(1, result.Metadata.PartitionSpecs);
        Assert.HasCount(1, result.Metadata.PartitionSpecs[0].Fields);
        Assert.AreEqual("id_bucket", result.Metadata.PartitionSpecs[0].Fields[0].Name);
        Assert.AreEqual("bucket[16]", result.Metadata.PartitionSpecs[0].Fields[0].Transform);
    }

    [TestMethod]
    public async Task CreateTable_AlreadyExists_ThrowsInvalidOperation()
    {
        await CreateTestNamespace();
        var request = new CreateTableRequest("freeze-ray-targets", SimpleSchema);
        await _catalog.CreateTableAsync(TestNamespace, request);

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => _catalog.CreateTableAsync(TestNamespace, request));
    }

    [TestMethod]
    public async Task CreateTable_NamespaceNotFound_ThrowsDirectoryNotFound()
    {
        await Assert.ThrowsExactlyAsync<DirectoryNotFoundException>(
            () => _catalog.CreateTableAsync(["ghost-glacier"], new CreateTableRequest("phantom-table", SimpleSchema)));
    }

    [TestMethod]
    public async Task LoadTable_Exists_ReturnsMetadata()
    {
        await CreateTestNamespace();
        var request = new CreateTableRequest("arctic-temperatures", SimpleSchema);
        var created = await _catalog.CreateTableAsync(TestNamespace, request);

        var loaded = await _catalog.LoadTableAsync(TestNamespace, "arctic-temperatures");

        Assert.AreEqual(created.Metadata.TableUuid, loaded.Metadata.TableUuid);
        Assert.AreEqual(2, loaded.Metadata.FormatVersion);
        Assert.AreEqual(-1, loaded.Metadata.CurrentSnapshotId);
        Assert.HasCount(2, loaded.Metadata.Schemas[0].Fields);
    }

    [TestMethod]
    public async Task LoadTable_NotFound_ThrowsFileNotFound()
    {
        await CreateTestNamespace();

        await Assert.ThrowsExactlyAsync<FileNotFoundException>(
            () => _catalog.LoadTableAsync(TestNamespace, "missing-iceberg"));
    }

    [TestMethod]
    public async Task LoadTable_NamespaceNotFound_ThrowsDirectoryNotFound()
    {
        await Assert.ThrowsExactlyAsync<DirectoryNotFoundException>(
            () => _catalog.LoadTableAsync(["ghost-glacier"], "phantom-table"));
    }

    [TestMethod]
    public async Task DropTable_Exists_RemovesDirectory()
    {
        await CreateTestNamespace();
        await _catalog.CreateTableAsync(TestNamespace, new CreateTableRequest("meltdown-data", SimpleSchema));

        await _catalog.DropTableAsync(TestNamespace, "meltdown-data");

        var list = await _catalog.ListTablesAsync(TestNamespace);
        Assert.IsEmpty(list.Identifiers);
    }

    [TestMethod]
    public async Task DropTable_NotFound_ThrowsFileNotFound()
    {
        await CreateTestNamespace();

        await Assert.ThrowsExactlyAsync<FileNotFoundException>(
            () => _catalog.DropTableAsync(TestNamespace, "never-frozen"));
    }

    [TestMethod]
    public async Task FullCrudCycle_CreateListLoadDrop()
    {
        await CreateTestNamespace();

        // Create two tables
        await _catalog.CreateTableAsync(TestNamespace,
            new CreateTableRequest("freeze-ray-targets", SimpleSchema));
        await _catalog.CreateTableAsync(TestNamespace,
            new CreateTableRequest("ice-crystal-inventory", SimpleSchema));

        // List — should have 2
        var list = await _catalog.ListTablesAsync(TestNamespace);
        Assert.HasCount(2, list.Identifiers);

        // Load one
        var loaded = await _catalog.LoadTableAsync(TestNamespace, "freeze-ray-targets");
        Assert.AreEqual(2, loaded.Metadata.FormatVersion);

        // Drop one
        await _catalog.DropTableAsync(TestNamespace, "freeze-ray-targets");

        // List — should have 1
        var afterDrop = await _catalog.ListTablesAsync(TestNamespace);
        Assert.HasCount(1, afterDrop.Identifiers);
        Assert.AreEqual("ice-crystal-inventory", afterDrop.Identifiers[0].Name);
    }
}
