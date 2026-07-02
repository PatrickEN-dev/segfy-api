# Task 02 — Camada Domain

## Objetivo

Implementar núcleo do domínio: `Policy` (aggregate root, com padrão de materialização EF-friendly), 5 Value Objects, `PolicyStatus` + máquina de estado, `IPolicyRepository`, hierarquia `DomainException`. **Zero dependências externas** — `Segfy.Domain.csproj` sem nenhum `PackageReference`.

## Prerequisites

- `task-01` DONE.

## Files to create

### `src/Segfy.Domain/Common/`

- `Entity.cs`:

  ```csharp
  public abstract class Entity
  {
      public Guid Id { get; protected set; }

      public override bool Equals(object? obj) => obj is Entity e && Id == e.Id;
      public override int GetHashCode() => Id.GetHashCode();
  }
  ```

  Nota: `protected set` permite que EF setar via reflection e que `Policy` setar no constructor factory.

- `AggregateRoot.cs`:

  ```csharp
  public abstract class AggregateRoot : Entity { }
  ```

  Marker semântico.

### `src/Segfy.Domain/Common/Errors/`

- `DomainException.cs` — `abstract`, `public string Code { get; }`.
- `DomainValidationException.cs` — `Code = "DOMAIN_VALIDATION"`.
- `DomainInvalidStateException.cs` — `Code = "INVALID_STATE"`.
- `DomainNotFoundException.cs` — `Code = "NOT_FOUND"`.

### `src/Segfy.Domain/Policies/ValueObjects/`

**Todos são `sealed record`. Construtor privado. Duas factories públicas: `Create` (valida) e `LoadTrusted` (reidratação sem revalidar, usado pelo EF Core value converter).**

#### `Document.cs`

```csharp
public sealed record Document
{
    public string Digits { get; }
    private Document(string digits) { Digits = digits; }
    public static Document Create(string raw);         // valida mod-11
    public static Document LoadTrusted(string digits); // sem validação
}
```

- `Create`: strip `[^\d]`, exigir 11 (CPF) ou 14 (CNPJ), rejeitar sequências repetidas (`00000000000` etc), validar mod-11.
  - **CPF**: 9 primeiros dígitos → pesos 10..2 → DV1; 10 dígitos → pesos 11..2 → DV2.
  - **CNPJ**: 12 primeiros dígitos → pesos [5,4,3,2,9,8,7,6,5,4,3,2] → DV1; 13 dígitos → pesos [6,5,4,3,2,9,8,7,6,5,4,3,2] → DV2.
- Falha → `DomainValidationException("Document is invalid.")`.

#### `LicensePlate.cs`

```csharp
public sealed record LicensePlate
{
    public string Value { get; }
    private LicensePlate(string value) { Value = value; }
    public static LicensePlate Create(string raw);
    public static LicensePlate LoadTrusted(string value);
}
```

- `Create`: uppercase + strip espaços.
  - Antigo: regex `^[A-Z]{3}-?[0-9]{4}$` → normalizar removendo hífen → `AAA9999`.
  - Mercosul: regex `^[A-Z]{3}[0-9][A-Z][0-9]{2}$` → manter.
- Nenhum bate → `DomainValidationException("License plate is invalid.")`.

#### `PolicyNumber.cs`

```csharp
public sealed record PolicyNumber
{
    public string Value { get; }        // "SEG-2026-0001"
    public int Year { get; }
    public int Sequential { get; }
    private PolicyNumber(int year, int seq, string value);
    public static PolicyNumber Create(int year, int sequential);
    public static PolicyNumber Parse(string value);  // reidratação (usado pelo EF converter)
}
```

- `Create`: `year in [1900, 9999]` e `sequential >= 1`. Formato `$"SEG-{year:D4}-{sequential:D4}"`. Padding expande além de 9999.
- `Parse`: regex `^SEG-(\d{4})-(\d{4,})$`. Extrai `year` e `sequential`. Reconstrói via ctor privado. Falha → `DomainValidationException`.

#### `Money.cs`

```csharp
public sealed record Money
{
    public decimal Amount { get; }
    private Money(decimal amount) { Amount = amount; }
    public static Money Create(decimal amount);
    public static Money LoadTrusted(decimal amount);
}
```

- `Create`: exigir `> 0`; arredondar 2 casas com `MidpointRounding.AwayFromZero`.
- Falha → `DomainValidationException("Premium must be greater than zero.")`.

#### `CoveragePeriod.cs`

```csharp
public sealed record CoveragePeriod
{
    public DateOnly Start { get; }
    public DateOnly End { get; }
    private CoveragePeriod(DateOnly start, DateOnly end);
    public static CoveragePeriod Create(DateOnly start, DateOnly end);
    public static CoveragePeriod LoadTrusted(DateOnly start, DateOnly end);
}
```

- `Create`: exigir `end > start`.
- Falha → `DomainValidationException("CoverageEnd must be greater than CoverageStart.")`.

> **Importante**: `CoveragePeriod` **não é** persistida como owned entity. `Policy` guarda `CoverageStart` e `CoverageEnd` como colunas diretas e reconstrói o VO via computed property `Coverage`. Ver `specs/database.md §4`.

### `src/Segfy.Domain/Policies/`

#### `PolicyStatus.cs`

```csharp
public enum PolicyStatus { Ativa, Cancelada, Expirada }
```

#### `PolicyStatusExtensions.cs`

```csharp
public static class PolicyStatusExtensions
{
    public static bool CanTransitionTo(this PolicyStatus current, PolicyStatus next) =>
        (current, next) switch
        {
            (PolicyStatus.Ativa, PolicyStatus.Cancelada) => true,
            (PolicyStatus.Ativa, PolicyStatus.Expirada) => true,
            _ => false
        };
}
```

Transições permitidas: `Ativa → Cancelada`, `Ativa → Expirada`. Tudo mais rejeitado (inclusive `Ativa → Ativa`, `Cancelada → *`, `Expirada → *`).

#### `Policy.cs`

**Aggregate root. Padrão EF-friendly obrigatório.**

```csharp
public sealed class Policy : AggregateRoot
{
    // EF materialization — chamado por reflection, seta propriedades via reflection depois
    private Policy() { }

    // Ctor privado para as factories
    private Policy(
        Guid id,
        PolicyNumber number,
        Document document,
        LicensePlate licensePlate,
        Money premium,
        DateOnly coverageStart,
        DateOnly coverageEnd,
        PolicyStatus status,
        DateTime createdAt,
        DateTime updatedAt)
    {
        Id = id;
        Number = number;
        Document = document;
        LicensePlate = licensePlate;
        Premium = premium;
        CoverageStart = coverageStart;
        CoverageEnd = coverageEnd;
        Status = status;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    // Propriedades com `private set` + `= null!` para EF setar via reflection sem quebrar nullable analyzer
    public PolicyNumber Number { get; private set; } = null!;
    public Document Document { get; private set; } = null!;
    public LicensePlate LicensePlate { get; private set; } = null!;
    public Money Premium { get; private set; } = null!;
    public DateOnly CoverageStart { get; private set; }
    public DateOnly CoverageEnd { get; private set; }
    public PolicyStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    // Computed property: reconstrói o VO a partir das colunas diretas. Não é persistida (Ignored no EF config).
    public CoveragePeriod Coverage => CoveragePeriod.LoadTrusted(CoverageStart, CoverageEnd);

    public static Policy Create(
        PolicyNumber number,
        Document document,
        LicensePlate licensePlate,
        Money premium,
        CoveragePeriod coverage,
        DateTime nowUtc) =>
        new(Guid.NewGuid(), number, document, licensePlate, premium,
            coverage.Start, coverage.End, PolicyStatus.Ativa, nowUtc, nowUtc);

    // Reidratação em testes (fake repo). Não usado pelo EF (que materializa pelo parameterless ctor).
    public static Policy Load(
        Guid id,
        PolicyNumber number,
        Document document,
        LicensePlate licensePlate,
        Money premium,
        CoveragePeriod coverage,
        PolicyStatus status,
        DateTime createdAt,
        DateTime updatedAt) =>
        new(id, number, document, licensePlate, premium,
            coverage.Start, coverage.End, status, createdAt, updatedAt);

    public void UpdateDetails(
        Document document,
        LicensePlate licensePlate,
        Money premium,
        CoveragePeriod coverage,
        DateTime nowUtc)
    {
        Document = document;
        LicensePlate = licensePlate;
        Premium = premium;
        CoverageStart = coverage.Start;
        CoverageEnd = coverage.End;
        UpdatedAt = nowUtc;
    }

    public void ChangeStatus(PolicyStatus newStatus, DateTime nowUtc)
    {
        if (!Status.CanTransitionTo(newStatus))
            throw new DomainInvalidStateException(
                $"Cannot transition policy status from {Status} to {newStatus}.");
        Status = newStatus;
        UpdatedAt = nowUtc;
    }
}
```

### `src/Segfy.Domain/Policies/Abstractions/`

- `IPolicyRepository.cs`:

  ```csharp
  public interface IPolicyRepository
  {
      Task AddAsync(Policy policy, CancellationToken ct);
      Task<Policy?> FindByIdAsync(Guid id, CancellationToken ct);
      Task<IReadOnlyList<Policy>> ListAsync(int page, int pageSize, CancellationToken ct);
      Task<int> CountAsync(CancellationToken ct);
      Task UpdateAsync(Policy policy, CancellationToken ct);
      Task RemoveAsync(Policy policy, CancellationToken ct);
      Task<IReadOnlyList<Policy>> ListExpiringAsync(DateOnly today, int daysWindow, CancellationToken ct);
  }
  ```

## Files to modify

- Remover `_placeholder.cs` do `src/Segfy.Domain/`.

## Acceptance criteria

- [ ] `Segfy.Domain.csproj` sem nenhum `PackageReference` (grep vazio).
- [ ] Nenhum arquivo do Domain usa `using Microsoft.EntityFrameworkCore`, `using Microsoft.AspNetCore.*`, `using FluentValidation`.
- [ ] `Policy` tem **dois** construtores privados: parameterless (`private Policy() { }`) e o parameterizado usado pelas factories.
- [ ] Todas as properties de `Policy` são `{ get; private set; }` — nada init-only ou read-only.
- [ ] Properties de referência (`Number`, `Document`, `LicensePlate`, `Premium`) têm initializer `= null!` para satisfazer nullable analyzer.
- [ ] `Policy.Coverage` é uma **computed property** (só getter, sem setter, sem backing field explícito).
- [ ] Todos os 5 VOs têm `Create` e `LoadTrusted` (exceto `PolicyNumber` que usa `Parse` no papel de `LoadTrusted`).
- [ ] `Document.Create("529.982.247-25")` → `.Digits == "52998224725"`.
- [ ] `Document.Create("00000000000")` → `DomainValidationException`.
- [ ] `LicensePlate.Create("ABC-1234")` → `.Value == "ABC1234"`.
- [ ] `LicensePlate.Create("abc1d23")` → `.Value == "ABC1D23"`.
- [ ] `PolicyNumber.Create(2026, 42).Value == "SEG-2026-0042"`.
- [ ] `PolicyNumber.Parse("SEG-2026-0042")` → `Year == 2026, Sequential == 42`.
- [ ] `Money.Create(199.905m).Amount == 199.91m`.
- [ ] `Policy.Create(...)` → `Status == Ativa`, `Coverage.Start` e `Coverage.End` batem com o VO passado.
- [ ] `Policy.ChangeStatus` de `Cancelada` para qualquer coisa lança `DomainInvalidStateException`.

## Definition of Done

Ver template em `CLAUDE.md §Definition of Done`. Além disso:

- [ ] `Segfy.Domain.csproj` sem `PackageReference` (grep).
- [ ] `Policy` compila com `<Nullable>enable</Nullable>` e sem warnings.
