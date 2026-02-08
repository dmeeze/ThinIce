namespace Meeze.ThinIce.Iceberg;

public interface IIcebergStorageResolver
{
    IIcebergStorage GetStorage(string tenant);
}
