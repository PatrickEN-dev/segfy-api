using Microsoft.Extensions.DependencyInjection;
using Segfy.Application.UseCases.Policies;

namespace Segfy.Application;

public static class ApplicationDependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<CreatePolicyUseCase>();
        services.AddScoped<GetPolicyByIdUseCase>();
        services.AddScoped<ListPoliciesUseCase>();
        services.AddScoped<UpdatePolicyUseCase>();
        services.AddScoped<DeletePolicyUseCase>();
        services.AddScoped<GetExpiringPoliciesUseCase>();
        services.AddScoped<GetPolicyStatusHistoryUseCase>();
        services.AddScoped<ExpirePoliciesBatchUseCase>();
        return services;
    }
}
