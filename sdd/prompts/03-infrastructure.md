# 03 — Infrastructure (persistence)

```
Execute sdd/tasks/task-04.md (Infrastructure).

Pré-requisitos: task-03 DONE. dotnet-ef instalado (`dotnet ef --version`).

Foco: SegfyDbContext + PolicyConfiguration + PolicyNumberSequenceConfiguration + PolicyRepository + SqlitePolicyNumberSequence (transacional) + SystemClock + migration Init.

Mapeamento EF é o **canônico** de specs/database.md §3. Não invente alternativas.
ListExpiringAsync usa `FromSqlRaw` obrigatoriamente — nunca LINQ.

Regras universais e DoD em CLAUDE.md.
```

_Este prompt executa task-04 porque a ordem canônica de tarefas é Domain → Application → Infrastructure → Api. O filename `03-infrastructure` reflete a fase temática, não o número da task._
