# 05 — Api

```
Execute sdd/tasks/task-05.md.

Pré-requisitos: task-04 DONE.

Foco: PoliciesController com 6 endpoints (POST/GET/GET-id/PUT/DELETE/GET-expiring) + Request/Response records + FluentValidation validators + PolicyPresenter estático + Swagger em /docs + AddApiServices com AddOptions ValidateOnStart.

FluentValidation faz só forma (NotEmpty, MaximumLength, GreaterThan, comparação entre campos). Semântica (mod-11, regex de placa) fica no VO. Não duplique.

ValidationException ainda vira 500 nesta task; o middleware que traduz para 400 entra na task-06. Não improvise catch aqui.

Regras universais e DoD em CLAUDE.md.
```
