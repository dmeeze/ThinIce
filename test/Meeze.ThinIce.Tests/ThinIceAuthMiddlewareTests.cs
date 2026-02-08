using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Meeze.ThinIce.Auth;
using Meeze.ThinIce.Iceberg.LocalDev;

namespace Meeze.ThinIce.Tests;

[TestClass]
public class ThinIceAuthMiddlewareTests
{
    private static IHost CreateTestHost()
    {
        return new HostBuilder()
            .ConfigureWebHost(webBuilder =>
            {
                webBuilder.UseTestServer();
                webBuilder.ConfigureServices(services =>
                {
                    services.AddRouting();
                    var devOptions = Options.Create(new LocalDevOptions
                    {
                        Tokens = ["freeze-ray-token-001:SnowyConesIceCream:mrfreeze@example.com"]
                    });
                    services.AddSingleton(devOptions);
                    services.AddSingleton<IAuthProvider>(sp =>
                        new LocalDevAuthProvider(sp.GetRequiredService<IOptions<LocalDevOptions>>(),
                            NullLogger<LocalDevAuthProvider>.Instance));
                    services.AddThinIceAuth(auth =>
                    {
                        auth.AnonymousEndpoints =
                        [
                            new("/v1/config", "GET")
                        ];
                    });
                });
                webBuilder.Configure(app =>
                {
                    app.UseThinIceAuth();
                    app.UseRouting();
                    app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapGet("/v1/config", () => Results.Ok("config"));
                        endpoints.MapGet("/v1/namespaces", (HttpContext ctx) =>
                        {
                            var tenant = ctx.Features.Get<TenantContext>();
                            return Results.Ok(tenant?.Tenant ?? "none");
                        });
                    });
                });
            })
            .Build();
    }

    private static async Task<(IHost Host, HttpClient Client)> StartTestHostAsync()
    {
        var host = CreateTestHost();
        await host.StartAsync();
        return (host, host.GetTestClient());
    }

    [TestMethod]
    public async Task AnonymousEndpoint_NoAuth_Returns200()
    {
        var (host, client) = await StartTestHostAsync();
        using var _ = host;

        var response = await client.GetAsync("/v1/config");

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }

    [TestMethod]
    [DataRow(null, DisplayName = "No auth header")]
    [DataRow("bogus-token", DisplayName = "Invalid token")]
    public async Task ProtectedEndpoint_BadOrMissingAuth_Returns401(string? token)
    {
        var (host, client) = await StartTestHostAsync();
        using var _ = host;

        if (token is not null)
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/v1/namespaces");

        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task ProtectedEndpoint_ValidToken_Returns200WithTenant()
    {
        var (host, client) = await StartTestHostAsync();
        using var _ = host;
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "freeze-ray-token-001");

        var response = await client.GetAsync("/v1/namespaces");

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        StringAssert.Contains(body, "SnowyConesIceCream");
    }
}
