# Implementation Order

Execução **linear e estrita**. Uma tarefa por vez. Só avance quando a DoD atual estiver 100% verde.

## Ordem canônica

| # | Tarefa | Escopo |
|---|---|---|
| 01 | [task-01](./tasks/task-01.md) | Bootstrap (.sln, 4 projetos src + 2 tests, packages, Program.cs esqueleto) |
| 02 | [task-02](./tasks/task-02.md) | Domain: `Policy`, 5 VOs, `PolicyStatus` + máquina, `IPolicyRepository`, `DomainException` + 3 subclasses |
| 03 | [task-03](./tasks/task-03.md) | Application: 6 use cases, `IClock`, `IPolicyNumberSequence`, `SegfyOptions`, DTOs internos |
| 04 | [task-04](./tasks/task-04.md) | Infrastructure: `SegfyDbContext`, mapeamento canônico, `PolicyRepository`, `SqlitePolicyNumberSequence`, migration `Init` |
| 05 | [task-05](./tasks/task-05.md) | Api: `PoliciesController` (6 endpoints), DTOs, validators, presenter, Swagger |
| 06 | [task-06](./tasks/task-06.md) | Cross-cutting: Serilog, `ExceptionHandlingMiddleware`, health endpoint, `ValidateOnStart` |
| 07 | [task-07](./tasks/task-07.md) | Testes: VOs, `Policy`, use cases (fakes in-memory) |
| 08 | [task-08](./tasks/task-08.md) | README, `.gitignore`; Docker/seed opcionais |

## Grafo de dependências

```
01 → 02 → 03 → 04 → 05 → 06 → 07 → 08
```

Sequencial estrito. Nenhuma tarefa começa com a anterior incompleta.

## Encerramento

Após `task-08`, rode [`reviews/final-checklist.md`](./reviews/final-checklist.md). Item pendente → volte à task correspondente.
