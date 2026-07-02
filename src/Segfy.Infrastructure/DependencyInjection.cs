using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Segfy.Application.Abstractions;
using Segfy.Domain.Policies.Abstractions;
using Segfy.Infrastructure.Persistence;
using Segfy.Infrastructure.Persistence.Repositories;
using Segfy.Infrastructure.Persistence.Sequences;
using Segfy.Infrastructure.Time;

namespace Segfy.Infrastructure;

public static class InfrastructureDependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration cfg)
    {
        services.AddDbContext<SegfyDbContext>(o =>
            o.UseSqlite(cfg.GetConnectionString("Default")));
        services.AddSingleton<IClock, SystemClock>();
        services.AddScoped<IPolicyRepository, PolicyRepository>();
        services.AddScoped<IPolicyNumberSequence, SqlitePolicyNumberSequence>();
        return services;
    }
}
