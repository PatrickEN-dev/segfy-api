# CLAUDE.md — Operating Manual

**SDD_VERSION:** `1.2.0` — mudanças na especificação **exigem** bump aqui e nota no PR.

**Changelog 1.2.0:**
- `Policy` agora exige `private Policy() { }` parameterless para EF Core materializar. Propriedades com `private set` + `= null!` initializer.
- `CoveragePeriod` deixa de ser `OwnsOne`. `Policy` guarda `CoverageStart`/`CoverageEnd` como colunas diretas e expõe `Coverage` como computed property (`Ignore` no EF).
- Seed de dev (`SegfyDbSeeder`) promovido de opcional para **required** em `task-08` — evaluator vê `/expiring` com dados na primeira request.
- Pasta `agents/` removida (arquivos órfãos; protocolo já vive em CLAUDE.md).

**Leia este arquivo primeiro. Depois `PROJECT_CONTEXT.md`, as 5 `specs/`, `tasks/backlog.md`, e execute `tasks/task-01.md` → `task-08.md` em ordem estrita.**

---

## Antes de começar (verificação de ambiente)

Rode e reporte a saída antes da task-01. Se qualquer um falhar, PARE.

```bash
dotnet --version         # deve ser >= 8.0
dotnet ef --version      # deve ser >= 8.0
git --version
```

## Objetivo

Web API em **C# / ASP.NET Core (.NET 8)** para CRUD de apólices de seguro automóvel (SegFy hands-on JR). Endpoint especial "vencendo em 30 dias" via **SQL cru**. Swagger em `/docs`. Testes unitários. SQLite via EF Core 8. Sem auth. Sem front-end.

## Regras universais (aplicam-se a TODA tarefa)

- Uma tarefa por vez. Só avance quando a DoD atual estiver 100% verde.
- Após cada tarefa rode os quality gates (abaixo). Todos devem passar.
- Regras de negócio: **só** de `specs/product-spec.md`. Não invente.
- Português para o domínio (`Ativa`, `Cancelada`, `Expirada`, `Apólice`, `Segurado`). Inglês para o resto.
- Zero warnings. `TreatWarningsAsErrors=true` em todos os csproj.
- Sem `TODO`/`FIXME` nos arquivos que você criar.
- Comente pouco: só o "por quê" quando não-óbvio. Nomes bons > comentários.
- Não modifique nada dentro de `sdd/`.
- Não crie arquivos `.md` de status/plano/log no projeto alvo.
- Prefira arquivos completos numa passada a patches iterativos.
- **Nunca** chame método, propriedade ou lib que você não viu documentado neste pacote ou na doc oficial. Se em dúvida, cite fonte.

## Escape hatches (quando parar)

- Spec ambígua e caso não coberto por `PROJECT_CONTEXT.md §Assunções` → **PARE**, pergunte ao usuário ou proponha ADR nova em `decisions/`.
- Precisa de lib fora da stack fixa → **PARE**, proponha ADR nova em `decisions/` explicando por quê. Aguarde aprovação.
- Precisa quebrar qualquer regra desta seção → **PARE**, explique o motivo, aguarde aprovação. Nunca quebre silenciosamente.
- Ambiente falhou (build, test, format) → **NÃO** ajuste teste para passar. Corrija causa raiz.

## Protocolo de resposta ao fim de cada task

Ao concluir uma `task-XX.md`, sua última mensagem ao usuário deve conter:

1. DoD checklist da task marcado ou lista dos itens vermelhos com motivo.
2. Lista de arquivos criados e modificados (paths relativos).
3. Saída resumida de `dotnet build` e `dotnet test`.
4. Próxima task (`task-<XX+1>.md`) ou, se for a última, referência a `reviews/final-checklist.md`.

## Arquitetura

```
Segfy.sln
├── src/
│   ├── Segfy.Api             Controllers, Contracts, Validators, Middleware
│   ├── Segfy.Application     UseCases, Application DTOs, Abstractions
│   ├── Segfy.Domain          Entities, VOs, Repository interfaces, DomainException
│   └── Segfy.Infrastructure  DbContext, Repositories, Migrations, SystemClock
└── tests/
    ├── Segfy.Domain.Tests
    └── Segfy.Application.Tests
```

Dependência: `Api → Application → Domain`, `Infrastructure → Domain, Application`.
`Segfy.Domain` **não** referencia EF Core, ASP.NET, FluentValidation ou Serilog. Detalhes em `specs/architecture.md`.

## Stack permitida

✅ ASP.NET Core 8 (Controllers) · EF Core 8 + Sqlite + Design · Swashbuckle
✅ FluentValidation · Serilog + Sinks.Console + Formatting.Compact
✅ xUnit + FluentAssertions · Moq (só se fake in-memory não resolver)

## Stack proibida

❌ MediatR · CQRS · Event Bus / Sourcing · RabbitMQ / Kafka · Redis
❌ MassTransit · Microservices / gRPC · AutoMapper · Hangfire / Quartz
❌ Auth (JWT, OAuth, Identity) — não pedido pelo PDF

**Racional.** PDF exige 3 dias e valoriza domínio de arquitetura *"não se empolgue demais"*. Menos é mais. Se pensar em usar item ❌, releia `PROJECT_CONTEXT.md`.

## Definition of Done (mesmo template para toda task)

Cada task-XX.md acrescenta itens específicos. Os quatro abaixo aplicam-se sempre:

- [ ] Todos os "Files to create" existem com conteúdo real.
- [ ] Todos os "Files to modify" foram atualizados.
- [ ] `dotnet build -warnaserror` passa.
- [ ] `dotnet test` passa (ou N/A se a task ainda não tem testes).
- [ ] `dotnet format --verify-no-changes` passa.
- [ ] `tasks/backlog.md` marca a task como `DONE`.

## Critérios de conclusão do projeto

- [ ] Todos os endpoints de `specs/api-contract.md` retornam o status esperado.
- [ ] Swagger em `/docs` documenta 100% dos endpoints.
- [ ] `dotnet run --project src/Segfy.Api` sobe, aplica migrations SQLite (`segfy.db`), responde `/health`.
- [ ] `GET /api/v1/policies/expiring` usa **SQL cru** (`FromSqlRaw`) — não LINQ.
- [ ] Todos os VOs e todos os UseCases têm ≥ 1 teste.
- [ ] README executa em ≤ 3 comandos.
- [ ] `reviews/final-checklist.md` 100% marcado.
