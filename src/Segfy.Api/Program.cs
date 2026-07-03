using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Segfy.Api;
using Segfy.Api.Health;
using Segfy.Api.Middleware;
using Segfy.Application;
using Segfy.Application.Abstractions;
using Segfy.Application.Configuration;
using Segfy.Infrastructure;
using Segfy.Infrastructure.Persistence;
using Serilog;
using Serilog.Formatting.Compact;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(new CompactJsonFormatter())
    .Enrich.FromLogContext()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((ctx, cfg) => cfg
        .ReadFrom.Configuration(ctx.Configuration)
        .Enrich.FromLogContext()
        .WriteTo.Console(new CompactJsonFormatter()));

    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);
    builder.Services.AddApiServices(builder.Configuration);

    var app = builder.Build();

    app.UseSerilogRequestLogging();
    app.UseMiddleware<ExceptionHandlingMiddleware>();
    app.UseSwagger();
    app.UseSwaggerUI(o => o.RoutePrefix = "docs");
    app.MapControllers();
    app.MapSegfyHealth();

    using (var scope = app.Services.CreateScope())
    {
        var serviceProvider = scope.ServiceProvider;
        var db = serviceProvider.GetRequiredService<SegfyDbContext>();
        db.Database.Migrate();

        var options = serviceProvider.GetRequiredService<IOptions<SegfyOptions>>().Value;
        if (options.SeedSampleData)
        {
            var clock = serviceProvider.GetRequiredService<IClock>();
            var sequence = serviceProvider.GetRequiredService<IPolicyNumberSequence>();
            await SegfyDbSeeder.SeedSampleAsync(db, clock, sequence, CancellationToken.None);
        }
    }

    app.Run();
    return 0;
}
#pragma warning disable CA1031 // Fatal boot failure — must catch everything to log and flush.
// HostAbortedException is how `dotnet ef` stops the host at design time; it is
// not a boot failure and must not be swallowed (or logged as fatal).
catch (Exception ex) when (ex is not HostAbortedException)
#pragma warning restore CA1031
{
    Log.Fatal(ex, "Boot failed");
    return 1;
}
finally
{
    Log.CloseAndFlush();
}

// Exposed as partial for WebApplicationFactory<Program> in integration tests.
public partial class Program;
