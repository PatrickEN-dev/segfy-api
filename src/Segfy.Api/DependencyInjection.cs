using FluentValidation;
using Microsoft.OpenApi.Models;
using Segfy.Api.HostedServices;
using Segfy.Api.Validators;
using Segfy.Application.Configuration;

namespace Segfy.Api;

public static class ApiDependencyInjection
{
    public static IServiceCollection AddApiServices(this IServiceCollection services, IConfiguration cfg)
    {
        services.AddControllers();
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(o => o.SwaggerDoc(
            "v1",
            new OpenApiInfo { Title = "Segfy Policies API", Version = "v1" }));
        services.AddValidatorsFromAssemblyContaining<CreatePolicyRequestValidator>();
        services.AddOptions<SegfyOptions>()
            .Bind(cfg.GetSection("Segfy"))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddHostedService<PolicyExpirationHostedService>();
        return services;
    }
}
