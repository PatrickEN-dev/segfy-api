# Task 08 — README, Seed, Ergonomia

## Objetivo

`README.md` na raiz do repositório alvo (≤ 3 comandos para rodar), `.gitignore` completo, e **`SegfyDbSeeder`** obrigatório para o avaliador ver `/expiring` funcionando na primeira request. Docker é opcional.

## Prerequisites

- `task-07` DONE.

## Files to create

### `src/Segfy.Infrastructure/Persistence/SegfyDbSeeder.cs`

**Obrigatório.** Popula 6 apólices variadas no primeiro boot em Development. Usa a mesma `SqlitePolicyNumberSequence` real (numeração consistente).

```csharp
public static class SegfyDbSeeder
{
    public static async Task SeedDevAsync(
        SegfyDbContext db,
        IClock clock,
        IPolicyNumberSequence seq,
        CancellationToken ct)
    {
        if (await db.Policies.AnyAsync(ct)) return;

        var today = clock.TodayUtc;
        var year = clock.UtcNow.Year;

        // 6 apólices: cobrem casos que o avaliador vai testar imediatamente.
        // CPFs e CNPJs abaixo são numericamente válidos (mod-11).
        var seedRecipe = new (string Doc, string Plate, decimal Premium,
                              DateOnly Start, DateOnly End, PolicyStatus Status)[]
        {
            // Aparece em /expiring (vence em 5 dias)
            ("52998224725",    "ABC1234", 199.90m,
             today.AddMonths(-2), today.AddDays(5),  PolicyStatus.Ativa),

            // Aparece em /expiring (vence em 25 dias)
            ("39053344705",    "DEF2G34", 249.50m,
             today.AddMonths(-1), today.AddDays(25), PolicyStatus.Ativa),

            // NÃO aparece em /expiring (vence em 40 dias)
            ("11144477735",    "GHI5678", 320.00m,
             today.AddDays(-30), today.AddDays(40),  PolicyStatus.Ativa),

            // Cancelada (não aparece em /expiring mesmo dentro da janela)
            ("11222333000181", "JKL9012", 500.00m,
             today.AddMonths(-6), today.AddDays(20), PolicyStatus.Cancelada),

            // Expirada (vigência já no passado)
            ("06990590000123", "MNO3456", 150.00m,
             today.AddYears(-2), today.AddYears(-1), PolicyStatus.Expirada),

            // Ativa longa
            ("52998224725",    "PQR7A89", 400.00m,
             today.AddMonths(-3), today.AddDays(90), PolicyStatus.Ativa),
        };

        foreach (var r in seedRecipe)
        {
            var s = await seq.NextForYearAsync(year, ct);
            var number = PolicyNumber.Create(year, s);
            var doc = Document.Create(r.Doc);
            var plate = LicensePlate.Create(r.Plate);
            var premium = Money.Create(r.Premium);
            var coverage = CoveragePeriod.Create(r.Start, r.End);

            // Status inicial é sempre Ativa. Para Cancelada/Expirada, transiciona depois.
            var policy = Policy.Create(number, doc, plate, premium, coverage, clock.UtcNow);
            if (r.Status != PolicyStatus.Ativa)
                policy.ChangeStatus(r.Status, clock.UtcNow);

            db.Policies.Add(policy);
        }

        await db.SaveChangesAsync(ct);
    }
}
```

> **Nota sobre os documentos escolhidos.** Os CPFs `52998224725`, `39053344705`, `11144477735` e CNPJs `11222333000181`, `06990590000123` são numericamente válidos pelo mod-11. Se algum for rejeitado pelo `Document.Create`, o teste do próprio VO (task-07) pega o bug — não é aqui.

### Raiz do repositório alvo

- `README.md` (PT-BR, ≤ 100 linhas):

  ```markdown
  # Segfy Policies API

  CRUD Web API em C# / .NET 8 para apólices de seguro automóvel.
  Persistência SQLite via EF Core. Swagger em `/docs`.

  ## Como rodar

  Requisitos: .NET 8 SDK.

      git clone <repo>
      cd <repo>
      dotnet run --project src/Segfy.Api

  Swagger: <http://localhost:5000/docs> (ou porta indicada no console).
  Banco `segfy.db` é criado no primeiro boot com migration aplicada **e populado
  com 6 apólices de exemplo** (2 aparecendo em `/expiring`).

  ## Endpoints

  | Método | Rota | Função |
  |---|---|---|
  | POST   | /api/v1/policies          | Cria |
  | GET    | /api/v1/policies          | Lista (page, pageSize) |
  | GET    | /api/v1/policies/{id}     | Detalhe |
  | PUT    | /api/v1/policies/{id}     | Atualiza |
  | DELETE | /api/v1/policies/{id}     | Remove |
  | GET    | /api/v1/policies/expiring | Vence em ≤ 30 dias (SQL cru) |
  | GET    | /health                   | Liveness |

  ## Testes

      dotnet test

  ## Estrutura

      src/
        Segfy.Api             HTTP
        Segfy.Application     Use cases
        Segfy.Domain          Entities, VOs, business rules
        Segfy.Infrastructure  EF Core + SQLite
      tests/
        Segfy.Domain.Tests
        Segfy.Application.Tests

  ## Formato do número da apólice

  Gerado automaticamente no padrão `SEG-YYYY-XXXX` (ex.: `SEG-2026-0001`).

  ## Reset do banco (dev)

  Requer o global tool `dotnet-ef` (`dotnet tool install --global dotnet-ef`).

      dotnet ef database drop -f --project src/Segfy.Infrastructure --startup-project src/Segfy.Api
      dotnet run --project src/Segfy.Api
  ```

- `.gitignore` — padrão Visual Studio + `bin/`, `obj/`, `*.db`, `*.db-shm`, `*.db-wal`, `TestResults/`, `.vs/`.

## Files to modify

### `src/Segfy.Api/Program.cs`

Após `Migrate()`, dentro do mesmo `if (app.Environment.IsDevelopment())`:

```csharp
using var scope = app.Services.CreateScope();
var sp = scope.ServiceProvider;
sp.GetRequiredService<SegfyDbContext>().Database.Migrate();

var db = sp.GetRequiredService<SegfyDbContext>();
var clock = sp.GetRequiredService<IClock>();
var seq = sp.GetRequiredService<IPolicyNumberSequence>();
await SegfyDbSeeder.SeedDevAsync(db, clock, seq, CancellationToken.None);
```

Nota: `Program.cs` fica com `await` no bootstrap. Requer o `Main` (top-level) marcar await — o WebApplication.CreateBuilder pipeline top-level suporta await direto no `Program.cs`.

## Opcional (só se sobrar tempo)

### Dockerfile multistage

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore Segfy.sln \
 && dotnet publish src/Segfy.Api/Segfy.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app .
USER app
EXPOSE 8080
ENTRYPOINT ["dotnet", "Segfy.Api.dll"]
```

`.dockerignore`: `bin/`, `obj/`, `.git/`, `**/appsettings.Development.json`, `*.db`.

Se implementado, adicionar seção no README:

    ## Via Docker

        docker build -t segfy-api .
        docker run -p 8080:8080 segfy-api

## Acceptance criteria

- [ ] `README.md` ≤ 100 linhas, PT-BR, execução em ≤ 3 comandos.
- [ ] `.gitignore` cobre `bin/`, `obj/`, `*.db*`, `TestResults/`.
- [ ] `git status` limpo (sem `bin/`, `obj/`, `.db`).
- [ ] `SegfyDbSeeder.SeedDevAsync` executa no primeiro boot em Development.
- [ ] `GET /api/v1/policies` retorna 6 apólices imediatamente após primeiro `dotnet run` em Dev.
- [ ] `GET /api/v1/policies/expiring` retorna **exatamente 2** apólices (as com `CoverageEnd = today+5` e `today+25`), ordenadas ASC.
- [ ] Segundo boot: seeder detecta que já há apólices e **não** duplica.
- [ ] (Opcional Docker) `docker build -t segfy-api .` sem erros.

## Definition of Done

Ver template em `CLAUDE.md §Definition of Done`. Depois execute [`reviews/final-checklist.md`](../reviews/final-checklist.md) do começo ao fim.
