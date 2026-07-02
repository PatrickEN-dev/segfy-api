# 06 — Cross-cutting

_Nota: filename `06-persistence` é rótulo do scaffold do pacote; conteúdo desta task é cross-cutting (Serilog, exception middleware, health, options com ValidateOnStart)._

```
Execute sdd/tasks/task-06.md.

Pré-requisitos: task-05 DONE.

Foco: ExceptionHandlingMiddleware mapeando ValidationException / DomainNotFoundException / DomainInvalidStateException / DomainValidationException / Exception → HTTP conforme specs/architecture.md §5. Serilog com CompactJsonFormatter + FromLogContext + UseSerilogRequestLogging. Health endpoint dedicado. SegfyOptions com [Range(1,365)] e ValidateOnStart.

Payload de erro sempre {"error":{"code","message","requestId","details"}}. requestId = HttpContext.TraceIdentifier.

Regras universais e DoD em CLAUDE.md.
```
