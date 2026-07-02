# 04 — Application

```
Execute sdd/tasks/task-03.md (Application).

Pré-requisitos: task-02 DONE.

Foco: 6 use cases (Create, GetById, List, Update, Delete, GetExpiring) + IClock + IPolicyNumberSequence + SegfyOptions + DTOs de input + PaginatedResult<T> + AddApplication.

Use cases retornam Policy (nunca DTO HTTP). Levantam apenas DomainException e derivadas.
Segfy.Application.csproj referencia só Segfy.Domain + Microsoft.Extensions.Options.

Regras universais e DoD em CLAUDE.md.
```

_Filename `04-application` reflete fase temática. Executa task-03 porque no fluxo canônico Application vem antes de Infrastructure (o use case consome interface do Domain; a impl fica para task-04)._
