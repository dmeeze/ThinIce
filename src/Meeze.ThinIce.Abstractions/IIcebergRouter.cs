namespace Meeze.ThinIce.Iceberg;

public interface IIcebergRouter
{
    IIcebergProvider GetProvider(string tenant);
}