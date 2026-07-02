# SDD Package — Segfy Hands-On Back-end JR

Este diretório contém o **Specification Driven Development** completo para construir uma Web API em C# / ASP.NET Core que gerencia apólices de seguro automóvel. Nenhum código C# aqui — só documentação executável.

## Como iniciar a construção

Abra este repositório no VS Code / IDE, inicie uma sessão nova do Claude e envie:

> **"Read `sdd/CLAUDE.md` and execute the implementation according to the SDD package."**

A sessão vai ler o pacote, executar as 8 tarefas em ordem e reportar no fim.

## Ordem de leitura recomendada

| # | Arquivo | Papel |
|---|---|---|
| 1 | [CLAUDE.md](./CLAUDE.md) | Manual de operação. **Primeiro arquivo a ler.** |
| 2 | [PROJECT_CONTEXT.md](./PROJECT_CONTEXT.md) | Escopo, o que reusar, o que ignorar, mapeamento de padrões |
| 3 | [IMPLEMENTATION_ORDER.md](./IMPLEMENTATION_ORDER.md) | Sequência canônica das tarefas |
| 4 | [specs/product-spec.md](./specs/product-spec.md) | Requisitos funcionais (só do PDF Segfy) |
| 5 | [specs/architecture.md](./specs/architecture.md) | Arquitetura alvo (camadas, DI, pipeline, testes) |
| 6 | [specs/api-contract.md](./specs/api-contract.md) | Contratos HTTP (request/response/status) |
| 7 | [specs/database.md](./specs/database.md) | EF Core + SQLite (mapeamento canônico dos VOs) |
| 8 | [specs/coding-guidelines.md](./specs/coding-guidelines.md) | Convenções de código C# |
| 9 | [tasks/backlog.md](./tasks/backlog.md) | Overview das 8 tarefas |
| 10 | [tasks/task-01.md](./tasks/task-01.md) → `task-08.md` | Tarefas em ordem estrita |
| 11 | [reviews/final-checklist.md](./reviews/final-checklist.md) | Gate final antes de entregar |
| 12 | [decisions/adr-001.md](./decisions/adr-001.md) | Estratégia de geração do número da apólice |

## Estrutura

```
sdd/
├── README.md                   este arquivo
├── CLAUDE.md                   manual de operação (primeiro a ler)
├── PROJECT_CONTEXT.md          escopo, libs permitidas/proibidas
├── IMPLEMENTATION_ORDER.md     ordem canônica de tarefas
├── specs/                      product · architecture · api-contract · database · coding-guidelines
├── tasks/                      backlog + task-01 ... task-08
├── prompts/                    00-master + 01-bootstrap ... 08-review
├── reviews/                    final-checklist
└── decisions/                  adr-001 (geração de número em SQLite)
```

## Alvo do projeto

Web API em **.NET 8** (C# 12, LTS) — atende requisito `>= .NET 6` do PDF. Persistência em **SQLite** via EF Core 8. CRUD de apólice de seguro automóvel + endpoint especial "vencendo em 30 dias" via **SQL cru**. Swagger em `/docs`. Testes unitários com xUnit + FluentAssertions.

Prazo do desafio: 3 dias.

## Regra de ouro

**Requisitos de negócio vêm exclusivamente do PDF, mapeados em `specs/product-spec.md`.**
**Padrões arquiteturais vêm exclusivamente das specs deste pacote.**
**Não invente nada que não esteja em nenhum dos dois.**
