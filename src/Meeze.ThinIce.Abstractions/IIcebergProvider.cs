namespace Meeze.ThinIce.Iceberg;

public interface IIcebergProvider
{
    bool CanHandle(string tenant);
    ICatalog GetCatalog(string tenant);
    IStorage GetStorage(string tenant);
}
