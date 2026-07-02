# Task 06 — Cross-cutting

## Objetivo

Serilog estruturado com `RequestId` enricher, middleware global de exceções mapeando domínio → HTTP conforme `specs/architecture.md §5`, health endpoint dedicado, options com `ValidateOnStart`.

## Prerequisites

- `task-05` DONE.

## Files to create

### `src/Segfy.Api/Middleware/`

- `ExceptionHandlingMiddleware.cs` — classe com `RequestDelegate _next`, `ILogger<ExceptionHandlingMiddleware> _logger`:

```csharp
public async Task InvokeAsync(HttpContext ctx)
{
    try { await _next(ctx); }
    catch (FluentValidation.ValidationException ex)
    {
        var details = ex.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
        await WriteError(ctx, 400, "VALIDATION_ERROR", "One or more validation errors occurred.", details);
    }
    catch (DomainNotFoundException ex)       { await WriteError(ctx, 404, ex.Code, ex.Message); }
    catch (DomainInvalidStateException ex)   { await WriteError(ctx, 422, ex.Code, ex.Message); }
    catch (DomainValidationException ex)     { await WriteError(ctx, 400, ex.Code, ex.Message); }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Unhandled exception.");
        await WriteError(ctx, 500, "INTERNAL_ERROR", "An unexpected error occurred.");
    }
}

private static Task WriteError(HttpContext ctx, int status, string code, string message,
                               IReadOnlyDictionary<string, string[]>? details = null)
{
    ctx.Response.StatusCode = status;
    ctx.Response.ContentType = "application/json";
    var body = new ErrorResponse(new ErrorBody(code, message, ctx.TraceIdentifier, details));
    return ctx.Response.WriteAsJsonAsync(body);
}
```

### `src/Segfy.Api/Health/`

- `HealthEndpointExtensions.cs`:
  ```csharp
  public static IEndpointRouteBuilder MapSegfyHealth(this IEndpointRouteBuilder e)
  {
      e.MapGet("/health", () => Results.Ok(new { status = "Healthy" }));
      return e;
  }
  ```

## Files to modify

### `src/Segfy.Api/Program.cs`

Bootstrap final:

```csharp
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(new Serilog.Formatting.Compact.CompactJsonFormatter())
    .Enrich.FromLogContext()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);
    builder.Host.UseSerilog((ctx, cfg) => cfg
        .ReadFrom.Configuration(ctx.Configuration)
        .Enrich.FromLogContext()
        .WriteTo.Console(new Serilog.Formatting.Compact.CompactJsonFormatter()));

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
        scope.ServiceProvider.GetRequiredService<SegfyDbContext>().Database.Migrate();
    }

    app.Run();
}
catch (Exception ex) { Log.Fatal(ex, "Boot failed"); }
finally { Log.CloseAndFlush(); }
```

### `src/Segfy.Api/appsettings.json`

Adicionar bloco:

```json
"Serilog": {
  "MinimumLevel": {
    "Default": "Information",
    "Override": {
      "Microsoft": "Warning",
      "Microsoft.Hosting.Lifetime": "Information",
      "Microsoft.EntityFrameworkCore.Database.Command": "Warning"
    }
  }
}
```

## Acceptance criteria

- [ ] `POST /api/v1/policies` com `premiumAmount: -10` retorna **400** `code="VALIDATION_ERROR"` com `details` por campo.
- [ ] `POST` com `document: "00000000000"` retorna **400** `code="DOMAIN_VALIDATION"`.
- [ ] `GET /api/v1/policies/{guid-inexistente}` retorna **404** `code="NOT_FOUND"`.
- [ ] `PUT /api/v1/policies/{id}` mudando status de policy `Cancelada` retorna **422** `code="INVALID_STATE"`.
- [ ] Todos os payloads de erro seguem `{"error":{"code","message","requestId","details"}}` com `requestId` não-vazio.
- [ ] Logs em JSON no console mostram `RequestId` (via `UseSerilogRequestLogging`).
- [ ] `/health` retorna 200 `{"status":"Healthy"}`.
- [ ] App falha no boot se `Segfy:ExpiringWindowDays` estiver fora de `[1, 365]`.

## Definition of Done

Ver template em `CLAUDE.md §Definition of Done`.
