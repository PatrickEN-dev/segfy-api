namespace Segfy.Api.Health;

public static class HealthEndpointExtensions
{
    public static IEndpointRouteBuilder MapSegfyHealth(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/health", () => Results.Ok(new { status = "Healthy" }));
        return endpoints;
    }
}
