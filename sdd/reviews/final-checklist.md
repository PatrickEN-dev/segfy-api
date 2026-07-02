# Final Checklist

Gate final antes de entregar. Nenhum item pode ficar sem `[x]`. Execute os comandos indicados; não assuma resultado.

## 1. Build & Test

- [ ] `dotnet --version` >= 8.0
- [ ] `dotnet ef --version` >= 8.0 (global tool instalada)
- [ ] `dotnet build Segfy.sln -warnaserror` → 0 warnings, 0 errors
- [ ] `dotnet test` → todos passam, tempo total < 5s
- [ ] `dotnet format --verify-no-changes` → OK

## 2. Estrutura de projetos

- [ ] `src/Segfy.{Api,Application,Domain,Infrastructure}/` existem
- [ ] `tests/Segfy.{Domain,Application}.Tests/` existem
- [ ] `Directory.Build.props` na raiz define `net8.0`, nullable, `TreatWarningsAsErrors=true`
- [ ] `Segfy.Domain.csproj` **sem** `PackageReference` externo (grep vazio)
- [ ] `Segfy.Application.csproj` referencia apenas `Segfy.Domain` + `Microsoft.Extensions.Options`

## 3. Comportamento HTTP (rode manualmente via Swagger ou curl)

Cada linha exige a request executada e a resposta observada.

- [ ] `POST /api/v1/policies` payload válido → **201** + Location header + `number` no padrão `SEG-YYYY-XXXX`
- [ ] Dois POSTs consecutivos no mesmo ano → segundo `number` incrementa (`SEG-2026-0001` → `SEG-2026-0002`)
- [ ] `POST` com `document = "00000000000"` → **400** `code = "DOMAIN_VALIDATION"`
- [ ] `POST` com `licensePlate = "INVALID!"` → **400** `code = "DOMAIN_VALIDATION"`
- [ ] `POST` com `premiumAmount = -10` → **400** `code = "VALIDATION_ERROR"` com `details`
- [ ] `POST` com `coverageEnd <= coverageStart` → **400**
- [ ] `GET /api/v1/policies/{uuid-random}` → **404** `code = "NOT_FOUND"`
- [ ] `GET /api/v1/policies` → **200** com `data[]` e `meta { page, pageSize, total, totalPages }`
- [ ] `PUT /api/v1/policies/{id}` com dados válidos → **200** com policy atualizada
- [ ] `PUT` com `status = "Ativa"` em policy `Cancelada` → **422** `code = "INVALID_STATE"`
- [ ] `DELETE /api/v1/policies/{id}` existente → **204**
- [ ] `DELETE` id inexistente → **404**
- [ ] `GET /api/v1/policies/expiring` → **200** com `meta.windowDays = 30` e `meta.reference = today`

## 4. Cenário completo do PDF (RF-06)

Criar 3 apólices via POST com `coverageEnd`:
- Uma em `today + 5` (Ativa)
- Uma em `today + 25` (Ativa)
- Uma em `today + 40` (Ativa)

- [ ] `GET /api/v1/policies/expiring` retorna **exatamente** as duas primeiras, ordenadas `coverageEnd ASC`
- [ ] Cancelar a de `today + 5` via PUT (status = `Cancelada`) → `GET expiring` agora retorna só a de `today + 25`

## 5. SQL cru confirmado

Habilite log EF em Information (`appsettings.Development.json`):

```json
"Microsoft.EntityFrameworkCore.Database.Command": "Information"
```

- [ ] `GET /api/v1/policies/expiring` imprime no console `SELECT * FROM Policies WHERE Status = ...` (SQL cru, não LINQ traduzido)
- [ ] `PolicyRepository.ListExpiringAsync` **grep** contém `FromSqlRaw`
- [ ] `PolicyRepository.ListExpiringAsync` **grep** não contém `.Where(` para filtro de status/data

## 6. Cross-cutting

- [ ] Toda resposta de erro tem `requestId` não-vazio
- [ ] Toda resposta de erro segue formato `{"error":{"code","message","requestId","details"}}`
- [ ] Nenhuma resposta 5xx expõe stack trace
- [ ] Logs no console mostram `RequestId` em cada request (via `UseSerilogRequestLogging`)
- [ ] `/health` → **200** `{"status":"Healthy"}`
- [ ] App **falha no boot** se `Segfy:ExpiringWindowDays = 0` (ValidateOnStart)

## 7. Testes cobrem o esperado

- [ ] Testes para cada VO (5 arquivos em `Segfy.Domain.Tests/Policies/ValueObjects/`)
- [ ] `PolicyTests` cobre invariantes e transições de status
- [ ] Testes para cada use case (6 arquivos em `Segfy.Application.Tests/UseCases/`)
- [ ] Cada use case tem ≥ 1 happy + ≥ 1 erro

## 8. Documentação e entrega

- [ ] `README.md` na raiz executa em ≤ 3 comandos
- [ ] `README.md` lista os 7 endpoints (6 policies + /health) e formato do número
- [ ] `README.md` mostra comando de testes
- [ ] `.gitignore` cobre `bin/`, `obj/`, `*.db*`, `TestResults/`, `.vs/`
- [ ] `git status` não mostra arquivos de build ou banco
- [ ] Repositório é **público** no GitHub
- [ ] Sem secrets vazados (checar `git log -p` para strings suspeitas)

## Encerramento

Se **todos** os itens acima estão `[x]`:

- Envie o link do repositório para **`novostalentos@segfy.com`**.
- Assunto **exato**: `DESENVOLVEDOR(A) JR — HANDS-ON`.

Se **um** item ficou `[ ]`: **não envie**. Volte à task correspondente e corrija.
