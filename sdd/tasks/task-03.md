# Task 03 — Camada Application

## Objetivo

Implementar 6 use cases + portas (`IClock`, `IPolicyNumberSequence`) + DTOs de input + `SegfyOptions`. Sem impl concreta das portas (é de task-04).

## Prerequisites

- `task-02` DONE.

## Files to create

### `src/Segfy.Application/Abstractions/`

- `IClock.cs`:
  - `DateTime UtcNow { get; }`
  - `DateOnly TodayUtc { get; }`

- `IPolicyNumberSequence.cs`:
  - `Task<int> NextForYearAsync(int year, CancellationToken ct);`

### `src/Segfy.Application/DTOs/`

- `CreatePolicyInput.cs` — `sealed record CreatePolicyInput(string Document, string LicensePlate, decimal PremiumAmount, DateOnly CoverageStart, DateOnly CoverageEnd);`
- `UpdatePolicyInput.cs` — `sealed record UpdatePolicyInput(string Document, string LicensePlate, decimal PremiumAmount, DateOnly CoverageStart, DateOnly CoverageEnd, string Status);`

### `src/Segfy.Application/Common/`

- `PaginatedResult.cs` — `sealed record PaginatedResult<T>(IReadOnlyList<T> Data, int Page, int PageSize, int Total);`

### `src/Segfy.Application/Configuration/`

- `SegfyOptions.cs` — POCO:
  ```csharp
  public sealed class SegfyOptions
  {
      [Range(1, 365)]
      public int ExpiringWindowDays { get; init; } = 30;
  }
  ```

### `src/Segfy.Application/UseCases/Policies/`

Cada use case é `sealed class`, construtor recebe dependências via primary constructor, método público `ExecuteAsync`.

- **`CreatePolicyUseCase`** — deps: `IPolicyRepository, IPolicyNumberSequence, IClock`.
  ```
  1. doc = Document.Create(input.Document)
  2. plate = LicensePlate.Create(input.LicensePlate)
  3. premium = Money.Create(input.PremiumAmount)
  4. coverage = CoveragePeriod.Create(input.CoverageStart, input.CoverageEnd)
  5. year = _clock.UtcNow.Year
  6. seq = await _seq.NextForYearAsync(year, ct)
  7. number = PolicyNumber.Create(year, seq)
  8. policy = Policy.Create(number, doc, plate, premium, coverage, _clock.UtcNow)
  9. await _repo.AddAsync(policy, ct)
  10. return policy
  ```

- **`GetPolicyByIdUseCase`** — deps: `IPolicyRepository`.
  ```
  policy = await _repo.FindByIdAsync(id, ct) ?? throw new DomainNotFoundException($"Policy {id} not found.");
  return policy;
  ```

- **`ListPoliciesUseCase`** — deps: `IPolicyRepository`.
  ```
  page = Math.Max(1, page);
  pageSize = Math.Clamp(pageSize, 1, 100);
  var (data, total) = (await _repo.ListAsync(page, pageSize, ct), await _repo.CountAsync(ct));
  return new PaginatedResult<Policy>(data, page, pageSize, total);
  ```

- **`UpdatePolicyUseCase`** — deps: `IPolicyRepository, IClock`.
  ```
  1. policy = await _repo.FindByIdAsync(id, ct) ?? throw new DomainNotFoundException(...);
  2. Reconstruir VOs de input (Document.Create, etc).
  3. policy.UpdateDetails(doc, plate, premium, coverage, _clock.UtcNow);
  4. desiredStatus = Enum.Parse<PolicyStatus>(input.Status, ignoreCase: false);
  5. if (desiredStatus != policy.Status) policy.ChangeStatus(desiredStatus, _clock.UtcNow);
  6. await _repo.UpdateAsync(policy, ct);
  7. return policy;
  ```

- **`DeletePolicyUseCase`** — deps: `IPolicyRepository`.
  ```
  policy = await _repo.FindByIdAsync(id, ct) ?? throw new DomainNotFoundException(...);
  await _repo.RemoveAsync(policy, ct);
  ```

- **`GetExpiringPoliciesUseCase`** — deps: `IPolicyRepository, IClock, IOptions<SegfyOptions>`.
  ```
  return await _repo.ListExpiringAsync(_clock.TodayUtc, _options.Value.ExpiringWindowDays, ct);
  ```

### `src/Segfy.Application/DependencyInjection.cs`

```csharp
public static IServiceCollection AddApplication(this IServiceCollection s)
{
    s.AddScoped<CreatePolicyUseCase>();
    s.AddScoped<GetPolicyByIdUseCase>();
    s.AddScoped<ListPoliciesUseCase>();
    s.AddScoped<UpdatePolicyUseCase>();
    s.AddScoped<DeletePolicyUseCase>();
    s.AddScoped<GetExpiringPoliciesUseCase>();
    return s;
}
```

## Files to modify

Nenhum.

## Package references

`Segfy.Application.csproj` referencia:
- `Segfy.Domain` (ProjectReference)
- `Microsoft.Extensions.Options` (PackageReference — necessário para `IOptions<T>` e `[Range]`)

## Acceptance criteria

- [ ] `dotnet build` verde.
- [ ] Nenhum use case importa EF Core ou ASP.NET.
- [ ] Cada use case tem exatamente um método público `ExecuteAsync`.
- [ ] Use cases retornam `Policy` (ou `Task` para Delete). Nunca DTO HTTP.
- [ ] `Segfy.Application.csproj` referencia apenas `Segfy.Domain` + `Microsoft.Extensions.Options`.

## Definition of Done

Ver template em `CLAUDE.md §Definition of Done`.
