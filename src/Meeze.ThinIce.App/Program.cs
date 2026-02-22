using Meeze.ThinIce.App.Endpoints;
using Meeze.ThinIce.Auth;
using Meeze.ThinIce.Iceberg;
using Meeze.ThinIce.Iceberg.LocalDev;
using Microsoft.AspNetCore.RateLimiting;

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

        builder.Services.AddHttpContextAccessor();
        builder.Services.AddSingleton<IIcebergRouter, DefaultIcebergRouter>();

        builder.Services.Configure<ConfigOptions>(options =>
        {
            var section = builder.Configuration.GetSection("Config");
            var prefix = section["Prefix"];
            if (prefix is not null)
                options.Prefix = prefix;
            var oauth2ServerUri = section["OAuth2ServerUri"];
            if (oauth2ServerUri is not null)
                options.OAuth2ServerUri = oauth2ServerUri;
        });

        builder.Services.Configure<ThrottlingOptions>(options =>
        {
            var section = builder.Configuration.GetSection("Throttling");
            if (int.TryParse(section["ConfigPermitsPerMinute"], out var configPermits))
                options.ConfigPermitsPerMinute = configPermits;
            if (int.TryParse(section["AuthenticatedPermitsPerMinute"], out var authPermits))
                options.AuthenticatedPermitsPerMinute = authPermits;
        });

        builder.Services.AddRateLimiter(limiter =>
        {
            var throttling = new ThrottlingOptions();
            var section = builder.Configuration.GetSection("Throttling");
            if (int.TryParse(section["ConfigPermitsPerMinute"], out var configPermits))
                throttling.ConfigPermitsPerMinute = configPermits;
            if (int.TryParse(section["AuthenticatedPermitsPerMinute"], out var authPermits))
                throttling.AuthenticatedPermitsPerMinute = authPermits;

            limiter.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            limiter.AddSlidingWindowLimiter("config", options =>
            {
                options.PermitLimit = throttling.ConfigPermitsPerMinute;
                options.Window = TimeSpan.FromMinutes(1);
                options.SegmentsPerWindow = 6;
                options.QueueLimit = 0;
            });

            limiter.AddSlidingWindowLimiter("authenticated", options =>
            {
                options.PermitLimit = throttling.AuthenticatedPermitsPerMinute;
                options.Window = TimeSpan.FromMinutes(1);
                options.SegmentsPerWindow = 6;
                options.QueueLimit = 0;
            });
        });

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
