# Database — EF Core 8 + SQLite

## 1. Motor

**SQLite** em arquivo (`segfy.db` no diretório de execução).
Connection string em `appsettings.Development.json`:

```json
"ConnectionStrings": { "Default": "Data Source=segfy.db;Cache=Shared" }
```

## 2. Tabelas

### 2.1 `Policies`

| Coluna | Tipo SQLite | Nullable | Origem no aggregate |
|---|---|---|---|
| `Id` | TEXT (Guid como string) | NO, PK | `Policy.Id` |
| `Number` | TEXT | NO, UNIQUE | `PolicyNumber.Value` (`SEG-2026-0001`) |
| `Document` | TEXT | NO | `Document.Digits` (só dígitos) |
| `LicensePlate` | TEXT | NO | `LicensePlate.Value` (uppercase, sem hífen) |
| `PremiumAmount` | TEXT | NO | `Money.Amount` serializado como `"F2"` InvariantCulture |
| `CoverageStart` | TEXT | NO | `Policy.CoverageStart` como `yyyy-MM-dd` |
| `CoverageEnd` | TEXT | NO | `Policy.CoverageEnd` como `yyyy-MM-dd` |
| `Status` | TEXT (max 20) | NO | `PolicyStatus` como string |
| `CreatedAt` | TEXT (ISO 8601 UTC) | NO | `Policy.CreatedAt` |
| `UpdatedAt` | TEXT (ISO 8601 UTC) | NO | `Policy.UpdatedAt` |

**Índice**: `IX_Policies_Number` UNIQUE em `Number`. Nada mais para o MVP.

> **Nota de design.** `CoverageStart` e `CoverageEnd` são colunas diretas em `Policy`, **não** owned entity. O aggregate expõe uma computed property `Policy.Coverage` que retorna `CoveragePeriod.LoadTrusted(CoverageStart, CoverageEnd)`. Esse desenho evita gotchas de `OwnsOne` com records em EF Core 8 (materialização por reflection de properties read-only é frágil). Domínio continua limpo (usa VO); persistência fica trivial.

### 2.2 `PolicyNumberSequences`

| Coluna | Tipo | Nullable |
|---|---|---|
| `Year` | INTEGER | NO, PK |
| `LastValue` | INTEGER | NO, default 0 |

Estratégia atômica em [ADR-001](../decisions/adr-001.md).

## 3. Mapeamento EF Core (canônico)

**Uma estratégia por VO. Sem alternativas.**

### `PolicyConfiguration : IEntityTypeConfiguration<Policy>`

```csharp
public sealed class PolicyConfiguration : IEntityTypeConfiguration<Policy>
{
    public void Configure(EntityTypeBuilder<Policy> builder)
    {
        builder.ToTable("Policies");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id).HasConversion<string>();

        builder.Property(p => p.Number)
            .HasConversion(v => v.Value, s => PolicyNumber.Parse(s))
            .HasColumnName("Number")
            .IsRequired();
        builder.HasIndex(p => p.Number).IsUnique();

        builder.Property(p => p.Document)
            .HasConversion(v => v.Digits, s => Document.LoadTrusted(s))
            .HasColumnName("Document")
            .IsRequired();

        builder.Property(p => p.LicensePlate)
            .HasConversion(v => v.Value, s => LicensePlate.LoadTrusted(s))
            .HasColumnName("LicensePlate")
            .IsRequired();

        builder.Property(p => p.Premium)
            .HasConversion(
                v => v.Amount.ToString("F2", CultureInfo.InvariantCulture),
                s => Money.LoadTrusted(decimal.Parse(s, CultureInfo.InvariantCulture)))
            .HasColumnName("PremiumAmount")
            .IsRequired();

        builder.Property(p => p.CoverageStart)
            .HasConversion(v => v.ToString("yyyy-MM-dd"), s => DateOnly.Parse(s))
            .HasColumnName("CoverageStart")
            .IsRequired();

        builder.Property(p => p.CoverageEnd)
            .HasConversion(v => v.ToString("yyyy-MM-dd"), s => DateOnly.Parse(s))
            .HasColumnName("CoverageEnd")
            .IsRequired();

        builder.Property(p => p.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(p => p.CreatedAt).IsRequired();
        builder.Property(p => p.UpdatedAt).IsRequired();

        // Computed property: reconstruída a partir de CoverageStart/End no getter. Nunca persistida.
        builder.Ignore(p => p.Coverage);
    }
}
```

### `PolicyNumberSequenceConfiguration : IEntityTypeConfiguration<PolicyNumberSequenceRow>`

```csharp
public sealed class PolicyNumberSequenceConfiguration : IEntityTypeConfiguration<PolicyNumberSequenceRow>
{
    public void Configure(EntityTypeBuilder<PolicyNumberSequenceRow> builder)
    {
        builder.ToTable("PolicyNumberSequences");
        builder.HasKey(x => x.Year);
        builder.Property(x => x.LastValue).HasDefaultValue(0).IsRequired();
    }
}
```

## 4. Materialização — regra crítica

Para EF Core 8 conseguir materializar `Policy` a partir do banco, o aggregate **precisa**:

1. **Parameterless constructor privado** (`private Policy() { }`). EF chama esse constructor via reflection, depois setta as propriedades.
2. **Propriedades com `private set`** (não read-only, não init-only). EF setta via reflection.
3. **Initializers `= null!`** nas propriedades de referência para satisfazer nullable analyzer.

Sem os três, EF joga `InvalidOperationException` em runtime na primeira query. Detalhes de implementação em [`../tasks/task-02.md`](../tasks/task-02.md).

Value Objects (records) **não** têm esse problema — são construídos via a função do value converter (`s => Document.LoadTrusted(s)`).

## 5. Notas SQLite

- **Sem `EnableRetryOnFailure`** — SQLite não precisa.
- **`decimal`**: sem tipo nativo. Storage como TEXT com `InvariantCulture` preserva precisão. `HasConversion<double>()` está **PROIBIDO** (perde precisão).
- **`DateOnly`**: `yyyy-MM-dd` — ordem lexicográfica == cronológica, funciona em SQL cru.
- **`Guid`**: string — portátil e legível em ferramentas.
- **`enum`**: string — legível em ferramentas.

## 6. Migrations

- Diretório: `src/Segfy.Infrastructure/Persistence/Migrations`
- Nome inicial: `Init`
- Aplicação automática só em Development:
  ```csharp
  if (app.Environment.IsDevelopment())
  {
      using var scope = app.Services.CreateScope();
      scope.ServiceProvider.GetRequiredService<SegfyDbContext>().Database.Migrate();
  }
  ```
- Em produção, `dotnet ef database update` manual.

## 7. Consulta de vencimento (RF-06) — SQL cru

`PolicyRepository.ListExpiringAsync`:

```csharp
public async Task<IReadOnlyList<Policy>> ListExpiringAsync(
    DateOnly today, int daysWindow, CancellationToken ct)
{
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
```

- `FromSqlRaw` com parâmetros por posição — EF Core parametriza como `SqliteParameter` automaticamente (safe contra injection).
- **Nunca** interpolar (`$""`) — quebra a parametrização.
- **Nunca** substituir por LINQ — o PDF exige "consulta SQL" literal.
- Como `CoverageStart`/`CoverageEnd` são colunas diretas na `Policies`, `SELECT *` retorna tudo que a materialização precisa (não há JOIN com owned entity).

## 8. Trade-offs (aceitos e documentados)

| Trade-off | Racional |
|---|---|
| Sequência commita antes do Policy insert; falha do Policy deixa gap na numeração | Aceitável para CRUD; gaps não violam formato SEG-YYYY-XXXX |
| Único índice UNIQUE em `Number`; sem composto Status+CoverageEnd | Volume de interview não justifica; expiring tem query direta em SQL |
| `decimal` como TEXT no SQLite | Preserva precisão; alternativa (`double`) perde precisão silenciosamente |
| `SaveChanges` dentro do repositório (sem UoW externo) | CRUD simples; expor UoW seria over-engineering |
| `CoveragePeriod` como computed property + colunas diretas | Evita fragilidade de `OwnsOne` com record read-only em EF Core 8 |

## 9. Reset em dev

```
dotnet ef database drop -f --project src/Segfy.Infrastructure --startup-project src/Segfy.Api
dotnet run --project src/Segfy.Api
```

Cross-platform. Recria com `Migrate()` + seed automático (ver [task-08](../tasks/task-08.md)).
