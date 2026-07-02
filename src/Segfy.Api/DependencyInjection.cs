using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using Segfy.Api.Contracts;
using Segfy.Api.HostedServices;
using Segfy.Api.Validators;
using Segfy.Application.Configuration;
using Segfy.Infrastructure.Persistence;

namespace Segfy.Api;

public static class ApiDependencyInjection
{
    public static IServiceCollection AddApiServices(this IServiceCollection services, IConfiguration cfg)
    {
        services.AddControllers();
        services.Configure<ApiBehaviorOptions>(o => o.InvalidModelStateResponseFactory = ctx =>
        {
            var details = ctx.ModelState
                .Where(kv => kv.Value?.Errors.Count > 0)
                .GroupBy(kv => string.IsNullOrEmpty(kv.Key) ? "request" : kv.Key)
                .ToDictionary(
                    g => g.Key,
                    g => g.SelectMany(kv => kv.Value!.Errors.Select(e => e.ErrorMessage)).ToArray());

            var body = new ErrorResponse(new ErrorBody(
                "VALIDATION_ERROR",
                "One or more validation errors occurred.",
                ctx.HttpContext.TraceIdentifier,
                details));
            return new BadRequestObjectResult(body) { ContentTypes = { "application/json" } };
        });

        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(o => o.SwaggerDoc(
            "v1", new OpenApiInfo { Title = "Segfy Policies API", Version = "v1" }));

        // Force English messages regardless of OS culture. Consistency with the rest of the API.
        ValidatorOptions.Global.LanguageManager.Enabled = false;
        services.AddValidatorsFromAssemblyContaining<CreatePolicyRequestValidator>();
        services.AddOptions<SegfyOptions>()
            .Bind(cfg.GetSection("Segfy"))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddHostedService<PolicyExpirationHostedService>();
        services.AddHealthChecks()
            .AddDbContextCheck<SegfyDbContext>(name: "database");
        return services;
    }
}
