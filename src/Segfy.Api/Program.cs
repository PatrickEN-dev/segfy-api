using Microsoft.EntityFrameworkCore;
using Segfy.Api;
using Segfy.Api.Health;
using Segfy.Api.Middleware;
using Segfy.Application;
using Segfy.Application.Abstractions;
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

    if (app.Environment.IsDevelopment())
    {
        using var scope = app.Services.CreateScope();
        var serviceProvider = scope.ServiceProvider;
        serviceProvider.GetRequiredService<SegfyDbContext>().Database.Migrate();

        var db = serviceProvider.GetRequiredService<SegfyDbContext>();
        var clock = serviceProvider.GetRequiredService<IClock>();
        var sequence = serviceProvider.GetRequiredService<IPolicyNumberSequence>();
        await SegfyDbSeeder.SeedDevAsync(db, clock, sequence, CancellationToken.None);
    }

    app.Run();
}
#pragma warning disable CA1031 // Fatal boot failure — must catch everything to log and flush.
catch (Exception ex)
#pragma warning restore CA1031
{
    Log.Fatal(ex, "Boot failed");
}
finally
{
    Log.CloseAndFlush();
}
