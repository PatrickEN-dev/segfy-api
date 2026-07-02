# Project Context

## Escopo

Simulação de trabalho para **Desenvolvedor(a) Back-end Jr — Segfy**. Web API em C# / ASP.NET Core para cadastro e consulta de **apólices de seguro automóvel**. Prazo: **3 dias**. Entrega: repositório público no GitHub + README claro. Sem "instalar mil coisas".

## Fontes de verdade

| Fonte | Autoridade |
|---|---|
| **PDF** (Segfy hands-on JR) | única fonte para **regras de negócio** |
| Este pacote SDD | única fonte para **arquitetura e convenções** |

Conflito → PDF vence.

## Padrões a REUSAR (arquiteturais)

- Camadas: `Domain` (puro) / `Application` (use cases) / `Infrastructure` (EF + services) / `Api` (HTTP)
- **Value Objects** para primitivos validados: `Document` (CPF/CNPJ mod-11), `LicensePlate` (antiga + Mercosul), `PolicyNumber` (`SEG-YYYY-XXXX`), `Money` (`decimal` encapsulado), `CoveragePeriod` (start < end)
- **Aggregate Root** `Policy` com factories `Create` e `Load`, sem setters públicos
- **Repository pattern**: interface no Domain, impl no Infrastructure
- **Use Case pattern**: 1 classe por operação, método público `ExecuteAsync`
- **State machine** no Status (`Ativa` → `Cancelada` | `Expirada`; terminais)
- **Global exception middleware** mapeando `DomainException` → HTTP status
- **Formato de erro** `{"error":{"code","message","requestId","details"}}`
- **Options pattern + ValidateOnStart** para envs
- **Structured logging** (Serilog) com `RequestId` enricher
- DTOs de request separados dos DTOs de use case; **Presenter** converte aggregate → response
- Prefixo global `/api/v1`; Swagger em `/docs`; health check simples em `/health`
- Testes unitários com **fake repository in-memory** para use cases; testes puros nos VOs

## Fora de escopo (nada disso é implementado)

Autenticação · soft delete · audit trail · rate limiter · CQRS · MediatR · event bus · Redis · Docker obrigatório · Docker Compose · Postman collection · CI · deploy · front-end · Microservices · gRPC

Lista completa de libs proibidas em `CLAUDE.md §Stack proibida`.

## Mapa de tradução (referência mental)

| Padrão | Alvo em .NET |
|---|---|
| NestJS + Express | ASP.NET Core Web API 8 (Controllers) |
| Prisma + Postgres | EF Core 8 + SQLite |
| Postgres `SEQUENCE` | Tabela `PolicyNumberSequences` (upsert transacional — ADR-001) |
| `class-validator` | FluentValidation (só presença/tamanho; semântica fica nos VOs) |
| `zod` (env) | Options pattern + `IValidateOptions` |
| Pino | Serilog + `Formatting.Compact` |
| Argon2 / JWT / refresh | N/A (sem auth) |
| Jest + Supertest | xUnit + FluentAssertions |
| `Money` centavos `int` | `Money` VO wrapping `decimal` (.NET-idiomático) |
| `@nestjs/swagger` | Swashbuckle |

## Decisões nossas (registradas)

- Runtime **.NET 8 LTS** (atende `>= .NET 6` do PDF; roda em qualquer máquina moderna)
- **SQLite** (não SQL Server) — arquivo local, zero setup, atende PDF "SQL Server ou SQLite"
- **`decimal`** para dinheiro (não centavos) — tipo nativo idiomático, precisão exata (128 bits)
- **Hard delete** (não soft) — YAGNI, PDF não pede
- Endpoint `/api/v1/policies/expiring` para a consulta SQL requerida
- `PolicyNumberSequences(Year, LastValue)` como estratégia de sequência — ver [ADR-001](./decisions/adr-001.md)

## Assunções (onde o PDF é omisso)

1. **Delete é hard**: se fosse soft, PDF seria explícito.
2. **Update é PUT** (substituição total do editável) — evita ambiguidade de PATCH parcial.
3. **Status inicial no cadastro é `Ativa`**.
4. **`Expirada`** é setada manualmente via PUT. Sem auto-flip por leitura no MVP.
5. **Prêmio**: `decimal > 0`, até 2 casas.
6. **Vigência**: `DateOnly`; `End > Start`.
7. **Consulta de vencimento**: apenas `Status = 'Ativa'` com `CoverageEnd BETWEEN today AND today+30d`, `ORDER BY CoverageEnd ASC`.
8. **Documento**: aceita CPF ou CNPJ no mesmo campo; VO decide pelo comprimento após strip de máscara.
9. **Placa**: aceita antiga (`AAA-9999` / `AAA9999`) e Mercosul (`AAA9A99`); persistida uppercase, sem hífen.
10. **Response**: documento sem máscara; número no formato `SEG-YYYY-XXXX`.
