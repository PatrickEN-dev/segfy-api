using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Segfy.Api.IntegrationTests;

public sealed class SegfyWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _dbPath;

    public SegfyWebApplicationFactory()
    {
        _dbPath = Path.Combine(
            Path.GetTempPath(),
            $"segfy-integration-{Guid.NewGuid():N}.db");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Testing environment: seeder does NOT run, background jobs stay off,
        // and each test file gets its own SQLite file.
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Default", $"Data Source={_dbPath};Cache=Shared");
        builder.UseSetting("Segfy:AutoExpirationEnabled", "false");
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing && File.Exists(_dbPath))
        {
            try { File.Delete(_dbPath); } catch (IOException) { /* best-effort cleanup */ }
        }
    }
}
