# Segfy Policies API

CRUD Web API em C# / .NET 8 para apolices de seguro automovel.
Persistencia SQLite via EF Core. Swagger em `/docs`.

## Como Rodar

Requisitos: .NET 8 SDK.

```bash
git clone <repo>
cd <repo>
dotnet run --project src/Segfy.Api
```

Swagger: <http://localhost:5000/docs> ou porta indicada no console.

O banco `segfy.db` e criado no primeiro boot em Development com migration
aplicada e 6 apolices de exemplo, incluindo 2 vencendo em ate 30 dias.

## Endpoints

| Metodo | Rota | Funcao |
|---|---|---|
| POST | `/api/v1/policies` | Cria |
| GET | `/api/v1/policies` | Lista com `page` e `pageSize` |
| GET | `/api/v1/policies/{id}` | Detalhe |
| PUT | `/api/v1/policies/{id}` | Atualiza |
| DELETE | `/api/v1/policies/{id}` | Remove |
| GET | `/api/v1/policies/expiring` | Vence em ate 30 dias via SQL cru |
| GET | `/health` | Liveness |

## Testes

```bash
dotnet test
```

## Estrutura

```text
src/
  Segfy.Api             HTTP
  Segfy.Application     Use cases
  Segfy.Domain          Entities, VOs, business rules
  Segfy.Infrastructure  EF Core + SQLite
tests/
  Segfy.Domain.Tests
  Segfy.Application.Tests
```

## Numero Da Apolice

Gerado automaticamente no padrao `SEG-YYYY-XXXX`, por exemplo `SEG-2026-0001`.

## Reset Do Banco

Requer o global tool `dotnet-ef`.

```bash
dotnet ef database drop -f --project src/Segfy.Infrastructure --startup-project src/Segfy.Api
dotnet run --project src/Segfy.Api
```
