using Meeze.ThinIce.App;
using Meeze.ThinIce.App.Endpoints;
using Meeze.ThinIce.Auth;
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
                new("/v1/config", "GET")
            ];
        });
        builder.Services.AddLocalDevProvider(builder.Configuration);

        var app = builder.Build();

        app.UseThinIceAuth();

        app.MapConfigEndpoints();
        app.MapNamespaceEndpoints();
        app.MapTableEndpoints();
        app.MapDataEndpoints();

        app.Run();
    }
}
