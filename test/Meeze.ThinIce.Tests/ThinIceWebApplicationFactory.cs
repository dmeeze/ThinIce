using Meeze.ThinIce.App;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace Meeze.ThinIce.Tests;

public class ThinIceWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), $"ThinIce_Test_{Guid.NewGuid():N}");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["LocalDev:BasePath"] = _tempDir,
                ["LocalDev:Tokens:0"] = "freeze-ray-token-001:SnowyConesIceCream:mrfreeze@example.com",
                ["LocalDev:Tokens:1"] = "ice-age-token-002:WayneEnterprises:batman@example.org"
            });
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing && Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }
    }
}
