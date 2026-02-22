using Meeze.ThinIce;
using Meeze.ThinIce.App.Endpoints;
using Meeze.ThinIce.Iceberg;
using Meeze.ThinIce.Iceberg.LocalDev;

namespace Meeze.ThinIce.App;

public partial class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateSlimBuilder(args);

        builder.Services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
        });

        builder.Services.AddThinIceAuth(auth =>
        {
            auth.AnonymousEndpoints =
            [
                new(IcebergRoutes.Config, "GET")
            ];
        });
        builder.Services.AddLocalDevProvider(builder.Configuration);
        builder.Services.AddSingleton<IIcebergRouter, DefaultIcebergRouter>();
        builder.Services.AddThinIceConfiguration(builder.Configuration);
        builder.Services.AddThinIceRateLimiting(builder.Configuration);

        var app = builder.Build();

        app.UseThinIceAuth();
        app.UseRateLimiter();

        var prefix = app.Configuration.GetSection("Config")["Prefix"];

        app.MapConfigEndpoints();
        app.MapNamespaceEndpoints(prefix);
        app.MapTableEndpoints(prefix);
        app.MapCatalogEndpoints(prefix);
        app.MapDataEndpoints(prefix);

        app.Run();
    }
}
