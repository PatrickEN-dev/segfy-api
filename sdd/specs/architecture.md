# Architecture

Arquitetura alvo em .NET 8. Padrões idiomáticos + DDD tático leve.

## 1. Solution

```
Segfy.sln
├── src/
│   ├── Segfy.Api/
│   ├── Segfy.Application/
│   ├── Segfy.Domain/
│   └── Segfy.Infrastructure/
└── tests/
    ├── Segfy.Domain.Tests/
    └── Segfy.Application.Tests/
```

### Referências

- `Api → Application, Infrastructure`
- `Application → Domain`
- `Infrastructure → Domain, Application`
- `Domain → (nada)`
- `Domain.Tests → Domain`
- `Application.Tests → Application, Domain`

### Namespaces

- `Segfy.Api.{Controllers, Contracts, Validators, Middleware, Presenters, Health}`
- `Segfy.Application.{UseCases.Policies, Abstractions, DTOs, Common, Configuration}`
- `Segfy.Domain.{Common, Common.Errors, Policies, Policies.ValueObjects, Policies.Abstractions}`
- `Segfy.Infrastructure.{Persistence, Persistence.Configurations, Persistence.Repositories, Persistence.Sequences, Time}`

## 2. Responsabilidades por camada

### Domain

Zero deps externas. Contém:
- `Policy` (aggregate root, classe selada — segue padrão EF-friendly: `private Policy() { }` parameterless + properties `{ get; private set; } = null!` para materialização, ver `database.md §4`)
- Value Objects (records selados com factories `Create` + `LoadTrusted`)
- Enum `PolicyStatus` + extension `CanTransitionTo`
- `IPolicyRepository` interface
- `DomainException` + 3 subclasses (`Validation`, `InvalidState`, `NotFound`)

**Nota crítica sobre `CoveragePeriod`.** É VO no domínio (usado em `Create`, `UpdateDetails`, exposto como computed property `Policy.Coverage`), mas **não** é persistido como `OwnsOne`. O aggregate guarda `CoverageStart`/`CoverageEnd` como colunas diretas e reconstrói o VO no getter. Elimina fragilidade de materialização de records read-only em EF 8.

### Application

Depende só de Domain + `Microsoft.Extensions.Options`. Contém:
- Use cases (1 classe por operação, método `ExecuteAsync`, retorna aggregate ou `Task`)
- Abstrações: `IClock`, `IPolicyNumberSequence`
- DTOs de input planos
- `SegfyOptions` (POCO com `ExpiringWindowDays`)

### Infrastructure

Implementa portas de Application e Domain. Contém:
- `SegfyDbContext` + `IEntityTypeConfiguration<T>` por entidade
- `PolicyRepository` (implementa `IPolicyRepository`)
- `SqlitePolicyNumberSequence` (implementa `IPolicyNumberSequence`)
- `SystemClock` (implementa `IClock`)
- Migrations em `Persistence/Migrations`

### Api

- Controllers finos (validar → delegar ao use case → presenter → HTTP)
- Contracts (Request/Response records)
- FluentValidation validators
- Middleware global de exceção
- Presenter estático

## 3. Composition root

`Program.cs` só orquestra. Cada camada expõe `AddXxx(this IServiceCollection)` em `DependencyInjection.cs` interno.

Ordem no `Program.cs`:

1. Bootstrap Serilog
2. `builder.Host.UseSerilog(...)`
3. `builder.Services.AddApplication()`
4. `builder.Services.AddInfrastructure(builder.Configuration)`
5. `builder.Services.AddApiServices(builder.Configuration)`
6. Build → middleware pipeline (abaixo) → migrations em Dev → run

### Lifetimes

- `DbContext` — Scoped (`AddDbContext`)
- Repositórios, sequência, use cases — Scoped
- `IClock` (SystemClock) — Singleton
- Validators (FluentValidation) — via `AddValidatorsFromAssemblyContaining<...>`

## 4. Middleware pipeline (ordem)

```
UseSerilogRequestLogging()
UseMiddleware<ExceptionHandlingMiddleware>()
UseSwagger()
UseSwaggerUI(o => o.RoutePrefix = "docs")
MapControllers()
MapSegfyHealth()
```

Sem `UseAuthentication`/`UseAuthorization` (sem auth). Sem `UseHttpsRedirection` (SQLite local, dev-first).

## 5. Exception handling

`ExceptionHandlingMiddleware` catch em cadeia:

| Exception | Status | Code | Notas |
|---|---|---|---|
| `FluentValidation.ValidationException` | 400 | `VALIDATION_ERROR` | `details` = campo → mensagens |
| `DomainValidationException` | 400 | `DOMAIN_VALIDATION` | do VO / aggregate |
| `DomainNotFoundException` | 404 | `NOT_FOUND` | |
| `DomainInvalidStateException` | 422 | `INVALID_STATE` | transição inválida |
| `Exception` (fallback) | 500 | `INTERNAL_ERROR` | logar `Error`; nunca serializar stack trace |

Response sempre `{"error":{"code","message","requestId","details"}}`, `requestId = HttpContext.TraceIdentifier`.

## 6. Logging

- Serilog via `builder.Host.UseSerilog(...)`
- Sink: `Console` com `CompactJsonFormatter`
- Enricher: `FromLogContext` (`RequestId` vem do `UseSerilogRequestLogging`)
- Nível: `Information` default; `Microsoft` → `Warning`
- **Nunca** logar `Document` puro. Se necessário, mascarar (`***.***.***-XX`).

## 7. Validação (duas camadas, sem sobreposição)

| Camada | Cobertura | Onde |
|---|---|---|
| FluentValidation (HTTP boundary) | `NotEmpty`, `MaximumLength`, `GreaterThan`, comparação entre campos | Api/Validators |
| Value Object (Domain) | mod-11, regex de placa, formato do número, invariantes | Domain/Policies/ValueObjects |

**Regra:** FV faz forma. VO faz semântica. Não repetir.

**Dispatch:** manual no controller — `await _validator.ValidateAsync(request, ct); if (!result.IsValid) throw new ValidationException(result.Errors);`. Sem `AddFluentValidationAutoValidation` (evita dep extra).

## 8. DTOs

- **Request** (Api/Contracts): `sealed record CreatePolicyRequest(...)` etc.
- **Application input** (Application/DTOs): `sealed record CreatePolicyInput(...)` — mesmo shape, independente.
- **Response** (Api/Contracts): `sealed record PolicyResponse(...)`.
- **Presenter** (Api/Presenters): `public static class PolicyPresenter { public static PolicyResponse ToResponse(Policy p) => ...; }`.

## 9. Repository

`IPolicyRepository` em `Segfy.Domain.Policies.Abstractions`:

```
Task AddAsync(Policy p, CancellationToken ct);
Task<Policy?> FindByIdAsync(Guid id, CancellationToken ct);
Task<IReadOnlyList<Policy>> ListAsync(int page, int pageSize, CancellationToken ct);
Task<int> CountAsync(CancellationToken ct);
Task UpdateAsync(Policy p, CancellationToken ct);
Task RemoveAsync(Policy p, CancellationToken ct);
Task<IReadOnlyList<Policy>> ListExpiringAsync(DateOnly today, int daysWindow, CancellationToken ct);
```

Implementação chama `SaveChangesAsync` dentro de cada método (Unit of Work implícito no repositório — simples e suficiente para CRUD).

`ListExpiringAsync` **obrigatoriamente** via `FromSqlRaw` (requisito literal do PDF).

## 10. Use Cases

Um por operação. Assinatura:

```
public sealed class CreatePolicyUseCase(IPolicyRepository repo, IPolicyNumberSequence seq, IClock clock)
{
    public Task<Policy> ExecuteAsync(CreatePolicyInput input, CancellationToken ct);
}
```

- Retorna aggregate (ou `Task` para delete). **Nunca** DTO HTTP.
- Depende só de abstrações.
- Levanta apenas `DomainException` e derivadas.

## 11. Configuration

`SegfyOptions` em `Segfy.Application.Configuration`:

- `[Range(1, 365)] public int ExpiringWindowDays { get; init; } = 30;`

Registro em `AddApiServices`:

```
services.AddOptions<SegfyOptions>()
        .Bind(cfg.GetSection("Segfy"))
        .ValidateDataAnnotations()
        .ValidateOnStart();
```

Falha rápida no boot se inválido.

## 12. Testing

- **Domain.Tests**: puros. VOs, invariantes de `Policy`. Sem mocks. FluentAssertions.
- **Application.Tests**: fakes escritos à mão — `InMemoryPolicyRepository`, `FakeClock`, `FakePolicyNumberSequence`. Cada use case: happy path + erro esperado.
- **Sem** testes de integração no MVP. `WebApplicationFactory` fica para depois.
- Tempo total < 5s.
- `Moq` só se um fake não puder ser servido à mão.

## 13. EF Core

- `SegfyDbContext` em `Segfy.Infrastructure.Persistence`.
- `IEntityTypeConfiguration<Policy>` por entidade — sem `OnModelCreating` inline gigante.
- Migrations em `src/Segfy.Infrastructure/Persistence/Migrations`.
- Comando: `dotnet ef migrations add <Name> --project src/Segfy.Infrastructure --startup-project src/Segfy.Api --output-dir Persistence/Migrations`.
- Detalhes de mapeamento (VOs, `decimal`, `DateOnly`, index) em [`database.md`](./database.md).

## 14. Estilo cross-cutting

- `sealed` por padrão.
- `record` para DTOs e VOs; `class` para entities/services/controllers.
- Nullable habilitado em todo csproj.
- `TreatWarningsAsErrors=true`.
- `Directory.Build.props` na raiz consolida `<LangVersion>latest`, nullable, warnings, analisadores.

## 15. Fluxo canônico (POST /policies)

```
Controller.Create(CreatePolicyRequest req)
  ├─ await _validator.ValidateAsync(req)                → ValidationException → 400
  ├─ input = new CreatePolicyInput(...)
  ├─ policy = await _createUseCase.ExecuteAsync(input)
  │    ├─ VOs.Create(...)                                → DomainValidationException → 400
  │    ├─ seq = await _sequence.NextForYearAsync(year)   → transação SQLite
  │    ├─ number = PolicyNumber.Create(year, seq)
  │    ├─ policy = Policy.Create(number, doc, plate, ..., clock.UtcNow)
  │    └─ await _repo.AddAsync(policy)                   → INSERT + SaveChanges
  └─ return CreatedAtAction(...) + PolicyPresenter.ToResponse(policy)
```
