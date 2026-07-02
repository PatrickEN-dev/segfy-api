# Task 05 — Camada Api

## Objetivo

`PoliciesController` com 6 endpoints, DTOs de request/response, validators FluentValidation, presenter estático, Swagger. Após esta task, todos os endpoints respondem via Swagger com contratos corretos (o middleware de erro entra na task-06 — nesta task, `ValidationException` ainda vira 500; ok).

## Prerequisites

- `task-04` DONE.

## Files to create

### `src/Segfy.Api/Contracts/`

Todos `sealed record`:

- `CreatePolicyRequest(string Document, string LicensePlate, decimal PremiumAmount, DateOnly CoverageStart, DateOnly CoverageEnd)`
- `UpdatePolicyRequest(string Document, string LicensePlate, decimal PremiumAmount, DateOnly CoverageStart, DateOnly CoverageEnd, string Status)`
- `PolicyResponse(Guid Id, string Number, string Document, string LicensePlate, decimal PremiumAmount, DateOnly CoverageStart, DateOnly CoverageEnd, string Status, DateTime CreatedAt, DateTime UpdatedAt)`
- `PageMeta(int Page, int PageSize, int Total, int TotalPages)`
- `PaginatedPoliciesResponse(IReadOnlyList<PolicyResponse> Data, PageMeta Meta)`
- `ExpiringMeta(int WindowDays, DateOnly Reference)`
- `ExpiringPoliciesResponse(IReadOnlyList<PolicyResponse> Data, ExpiringMeta Meta)`
- `ErrorBody(string Code, string Message, string RequestId, IReadOnlyDictionary<string, string[]>? Details)`
- `ErrorResponse(ErrorBody Error)`

### `src/Segfy.Api/Validators/`

- `CreatePolicyRequestValidator : AbstractValidator<CreatePolicyRequest>`:
  - `Document`: `NotEmpty().MaximumLength(20)`.
  - `LicensePlate`: `NotEmpty().MaximumLength(10)`.
  - `PremiumAmount`: `GreaterThan(0)`.
  - `CoverageStart` e `CoverageEnd`: `NotEmpty` implícito (DateOnly default é 0001-01-01; validar `> default(DateOnly)`).
  - `CoverageEnd`: `Must((req, end) => end > req.CoverageStart).WithMessage("CoverageEnd must be greater than CoverageStart.")`.
- `UpdatePolicyRequestValidator` — mesmas regras + `Status`: `NotEmpty().Must(s => s is "Ativa" or "Cancelada" or "Expirada").WithMessage("Status must be one of: Ativa, Cancelada, Expirada.")`.

**Não** validar mod-11 ou regex de placa aqui — isso é do VO.

### `src/Segfy.Api/Presenters/`

- `PolicyPresenter.cs`:
  ```csharp
  public static class PolicyPresenter
  {
      public static PolicyResponse ToResponse(Policy p) => new(
          p.Id,
          p.Number.Value,
          p.Document.Digits,
          p.LicensePlate.Value,
          p.Premium.Amount,
          p.Coverage.Start,
          p.Coverage.End,
          p.Status.ToString(),
          p.CreatedAt,
          p.UpdatedAt);
  }
  ```

### `src/Segfy.Api/Controllers/`

- `PoliciesController` — `[ApiController]`, `[Route("api/v1/[controller]")]`, `[Produces("application/json")]`.

Injeta: `IValidator<CreatePolicyRequest>`, `IValidator<UpdatePolicyRequest>`, os 6 use cases, `IClock`, `IOptions<SegfyOptions>`, `ILogger<PoliciesController>`.

Endpoints:

- `POST /` (Create):
  ```
  await _createValidator.ValidateAndThrowAsync(req, ct);
  var input = new CreatePolicyInput(...);
  var policy = await _create.ExecuteAsync(input, ct);
  return CreatedAtAction(nameof(GetById), new { id = policy.Id }, PolicyPresenter.ToResponse(policy));
  ```
- `GET /{id:guid}` (GetById): `Ok(PolicyPresenter.ToResponse(await _get.ExecuteAsync(id, ct)))`.
- `GET /` (List) — query params `page = 1`, `pageSize = 20`:
  ```
  var result = await _list.ExecuteAsync(page, pageSize, ct);
  var meta = new PageMeta(result.Page, result.PageSize, result.Total, (int)Math.Ceiling(result.Total / (double)result.PageSize));
  return Ok(new PaginatedPoliciesResponse(result.Data.Select(PolicyPresenter.ToResponse).ToList(), meta));
  ```
- `PUT /{id:guid}` (Update): valida req, mapeia, chama `_update.ExecuteAsync(id, input, ct)`, retorna 200 com response.
- `DELETE /{id:guid}`: `await _delete.ExecuteAsync(id, ct); return NoContent();`.
- `GET /expiring`:
  ```
  var policies = await _expiring.ExecuteAsync(ct);
  var meta = new ExpiringMeta(_options.Value.ExpiringWindowDays, _clock.TodayUtc);
  return Ok(new ExpiringPoliciesResponse(policies.Select(PolicyPresenter.ToResponse).ToList(), meta));
  ```

### `src/Segfy.Api/DependencyInjection.cs`

```csharp
public static IServiceCollection AddApiServices(this IServiceCollection s, IConfiguration cfg)
{
    s.AddControllers();
    s.AddEndpointsApiExplorer();
    s.AddSwaggerGen(o => o.SwaggerDoc("v1", new OpenApiInfo { Title = "Segfy Policies API", Version = "v1" }));
    s.AddValidatorsFromAssemblyContaining<CreatePolicyRequestValidator>();
    s.AddOptions<SegfyOptions>()
     .Bind(cfg.GetSection("Segfy"))
     .ValidateDataAnnotations()
     .ValidateOnStart();
    return s;
}
```

## Files to modify

- `src/Segfy.Api/Program.cs` — substituir a chamada direta a `AddControllers` por `AddApiServices(builder.Configuration)`. Adicionar `AddApplication()` e `AddInfrastructure(builder.Configuration)` antes.

## Acceptance criteria

- [ ] `dotnet build` verde.
- [ ] Swagger em `/docs` mostra os 6 endpoints do controller + `/health`.
- [ ] `POST /api/v1/policies` com payload válido retorna 201 + Location + `PolicyResponse` com `number` no formato `SEG-YYYY-XXXX`.
- [ ] `GET /api/v1/policies/{id}` retorna 200 para id existente.
- [ ] `GET /api/v1/policies?page=1&pageSize=20` retorna 200 com `meta`.
- [ ] `PUT /api/v1/policies/{id}` retorna 200 com policy atualizada.
- [ ] `DELETE /api/v1/policies/{id}` retorna 204.
- [ ] `GET /api/v1/policies/expiring` retorna 200 com `meta.windowDays` e `meta.reference`.

## Definition of Done

Ver template em `CLAUDE.md §Definition of Done`.
