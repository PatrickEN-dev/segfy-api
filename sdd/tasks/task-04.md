# Task 04 — Camada Infrastructure (Persistence)

## Objetivo

Implementar `SegfyDbContext`, mapeamentos EF Core canônicos (copiados de [`specs/database.md §3`](../specs/database.md#3-mapeamento-ef-core-canônico)), `PolicyRepository`, `SqlitePolicyNumberSequence` (atômico), `SystemClock`, migration `Init`. Ao final, `dotnet run` cria `segfy.db` com schema pronto e roda migrations.

## Prerequisites

- `task-03` DONE.
- Global tool `dotnet-ef` disponível (`dotnet ef --version`). Instalar se ausente: `dotnet tool install --global dotnet-ef`.

## Files to create

### `src/Segfy.Infrastructure/Time/`

- `SystemClock.cs` — `sealed`, implementa `IClock`:

  ```csharp
  public sealed class SystemClock : IClock
  {
      public DateTime UtcNow => DateTime.UtcNow;
      public DateOnly TodayUtc => DateOnly.FromDateTime(DateTime.UtcNow);
  }
  ```

### `src/Segfy.Infrastructure/Persistence/`

- `PolicyNumberSequenceRow.cs`:

  ```csharp
  public sealed class PolicyNumberSequenceRow
  {
      public int Year { get; set; }
      public int LastValue { get; set; }
  }
  ```

  Vive só em Infrastructure. Não vaza para Domain.

- `SegfyDbContext.cs`:

  ```csharp
  public sealed class SegfyDbContext : DbContext
  {
      public SegfyDbContext(DbContextOptions<SegfyDbContext> options) : base(options) { }

      public DbSet<Policy> Policies => Set<Policy>();
      public DbSet<PolicyNumberSequenceRow> PolicyNumberSequences => Set<PolicyNumberSequenceRow>();

      protected override void OnModelCreating(ModelBuilder modelBuilder)
      {
          modelBuilder.ApplyConfigurationsFromAssembly(typeof(SegfyDbContext).Assembly);
      }
  }
  ```

### `src/Segfy.Infrastructure/Persistence/Configurations/`

Ambos os arquivos com o código **exato** de `specs/database.md §3`. Não improvise:

- `PolicyConfiguration.cs`
- `PolicyNumberSequenceConfiguration.cs`

**Regras críticas** (já em `database.md`, repetidas para reforçar):

- `builder.Ignore(p => p.Coverage)` — computed property NÃO é persistida.
- `HasConversion<double>()` para `decimal` está PROIBIDO (perda de precisão).
- Money usa `InvariantCulture` no ToString/Parse.
- `CoverageStart`/`CoverageEnd` são colunas diretas (`HasConversion` em cada uma). **Não usar `OwnsOne`.**

### `src/Segfy.Infrastructure/Persistence/Repositories/`

- `PolicyRepository.cs`:

  ```csharp
  public sealed class PolicyRepository : IPolicyRepository
  {
      private readonly SegfyDbContext _db;
      public PolicyRepository(SegfyDbContext db) { _db = db; }

      public async Task AddAsync(Policy policy, CancellationToken ct)
      {
          _db.Policies.Add(policy);
          await _db.SaveChangesAsync(ct);
      }

      public Task<Policy?> FindByIdAsync(Guid id, CancellationToken ct) =>
          _db.Policies.FirstOrDefaultAsync(p => p.Id == id, ct);

      public async Task<IReadOnlyList<Policy>> ListAsync(int page, int pageSize, CancellationToken ct) =>
          await _db.Policies
              .AsNoTracking()
              .OrderByDescending(p => p.CreatedAt)
              .Skip((page - 1) * pageSize)
              .Take(pageSize)
              .ToListAsync(ct);

      public Task<int> CountAsync(CancellationToken ct) => _db.Policies.CountAsync(ct);

      public async Task UpdateAsync(Policy policy, CancellationToken ct)
      {
          _db.Policies.Update(policy);
          await _db.SaveChangesAsync(ct);
      }

      public async Task RemoveAsync(Policy policy, CancellationToken ct)
      {
          _db.Policies.Remove(policy);
          await _db.SaveChangesAsync(ct);
      }

      public async Task<IReadOnlyList<Policy>> ListExpiringAsync(
          DateOnly today, int daysWindow, CancellationToken ct)
      {
          // Bloco exato conforme specs/database.md §7. Não substituir por LINQ.
          var todayStr = today.ToString("yyyy-MM-dd");
          var cutoffStr = today.AddDays(daysWindow).ToString("yyyy-MM-dd");

          return await _db.Policies
              .FromSqlRaw(
                  @"SELECT * FROM Policies
                    WHERE Status = {0}
                      AND CoverageEnd >= {1}
                      AND CoverageEnd <= {2}
                    ORDER BY CoverageEnd ASC",
                  "Ativa", todayStr, cutoffStr)
              .AsNoTracking()
              .ToListAsync(ct);
      }
  }
  ```

  Notas de decisão:
  - `FindByIdAsync` retorna entidade **tracked** (sem `AsNoTracking()`) — necessário para `UpdateAsync` do use case funcionar sem re-attach explícito.
  - `Update(policy)` marca modificado; SQLite gera `UPDATE ... WHERE Id = ?` só com as colunas que EF detectou como diferentes.

### `src/Segfy.Infrastructure/Persistence/Sequences/`

- `SqlitePolicyNumberSequence.cs`:

  ```csharp
  public sealed class SqlitePolicyNumberSequence : IPolicyNumberSequence
  {
      private readonly SegfyDbContext _db;
      public SqlitePolicyNumberSequence(SegfyDbContext db) { _db = db; }

      public async Task<int> NextForYearAsync(int year, CancellationToken ct)
      {
          await using var tx = await _db.Database.BeginTransactionAsync(ct);

          var row = await _db.PolicyNumberSequences
              .FirstOrDefaultAsync(x => x.Year == year, ct);

          if (row is null)
          {
              row = new PolicyNumberSequenceRow { Year = year, LastValue = 1 };
              _db.PolicyNumberSequences.Add(row);
          }
          else
          {
              row.LastValue += 1;
          }

          await _db.SaveChangesAsync(ct);
          await tx.CommitAsync(ct);

          return row.LastValue;
      }
  }
  ```

  Racional completo em [ADR-001](../decisions/adr-001.md).

### `src/Segfy.Infrastructure/DependencyInjection.cs`

```csharp
public static class InfrastructureDependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection s, IConfiguration cfg)
    {
        s.AddDbContext<SegfyDbContext>(o =>
            o.UseSqlite(cfg.GetConnectionString("Default")));
        s.AddSingleton<IClock, SystemClock>();
        s.AddScoped<IPolicyRepository, PolicyRepository>();
        s.AddScoped<IPolicyNumberSequence, SqlitePolicyNumberSequence>();
        return s;
    }
}
```

### Migration

Gerar via CLI:

```
dotnet ef migrations add Init --project src/Segfy.Infrastructure --startup-project src/Segfy.Api --output-dir Persistence/Migrations
```

## Files to modify

- `src/Segfy.Api/Program.cs`:
  - `builder.Services.AddApplication();`
  - `builder.Services.AddInfrastructure(builder.Configuration);`
  - Dentro de `if (app.Environment.IsDevelopment())`:
    ```csharp
    using var scope = app.Services.CreateScope();
    scope.ServiceProvider.GetRequiredService<SegfyDbContext>().Database.Migrate();
    ```

## Acceptance criteria

- [ ] `dotnet build` verde.
- [ ] `dotnet run --project src/Segfy.Api` sobe sem erro. Nenhum `InvalidOperationException` de materialização.
- [ ] Arquivo `segfy.db` criado após primeiro boot em Development.
- [ ] Migration `Init` presente em `src/Segfy.Infrastructure/Persistence/Migrations/`.
- [ ] Grep em `PolicyRepository.ListExpiringAsync`: contém `FromSqlRaw`. **Não** contém `.Where(` para filtro de status/data.
- [ ] Grep em `PolicyConfiguration`: contém `builder.Ignore(p => p.Coverage)`.
- [ ] Grep em `SqlitePolicyNumberSequence`: contém `BeginTransactionAsync` e `CommitAsync`.
- [ ] `POST /api/v1/policies` seguido de `GET /api/v1/policies/{id}` **funciona** (sanity manual: se materialização quebrar, `GET` retorna 500).

## Definition of Done

Ver template em `CLAUDE.md §Definition of Done`. Além disso:

- [ ] `dotnet ef migrations list --project src/Segfy.Infrastructure --startup-project src/Segfy.Api` mostra `Init` aplicada.
- [ ] Arquivo `segfy.db` gerado no diretório de execução.
- [ ] Round-trip manual: POST cria uma apólice, GET a recupera pelo id, campos batem 1:1. Se falhar → materialização quebrou; volte à task-02.
