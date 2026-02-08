namespace Meeze.ThinIce.Iceberg;

public interface IIcebergCatalogResolver
{
    IIcebergCatalog GetCatalog(string tenant);
}
