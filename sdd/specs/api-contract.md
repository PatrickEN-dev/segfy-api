# API Contract

Prefixo global: `/api/v1`. Todos os endpoints retornam JSON com `application/json; charset=utf-8`.

Formato de erro padronizado em todas as respostas 4xx e 5xx:

```json
{
  "error": {
    "code": "STRING_UPPER_SNAKE",
    "message": "human-readable",
    "requestId": "0HN2...",
    "details": { }
  }
}
```

`requestId` = `HttpContext.TraceIdentifier`. Sempre presente.

---

## 1. POST /api/v1/policies

Cria uma apólice.

### Request

```json
{
  "document": "529.982.247-25",
  "licensePlate": "ABC-1234",
  "premiumAmount": 199.90,
  "coverageStart": "2026-07-01",
  "coverageEnd": "2027-06-30"
}
```

- `document` — CPF (11 dígitos) ou CNPJ (14 dígitos), com ou sem máscara. Validado por mod-11.
- `licensePlate` — placa brasileira antiga (`AAA-9999` / `AAA9999`) ou Mercosul (`AAA9A99`).
- `premiumAmount` — decimal > 0, até 2 casas.
- `coverageStart`, `coverageEnd` — `date` (ISO 8601, sem hora). `coverageEnd > coverageStart`.
- `status` — **não aceito no create**. Sempre inicializado como `Ativa`.

### Validation errors (400)

Exemplo:

```json
{
  "error": {
    "code": "VALIDATION_ERROR",
    "message": "One or more validation errors occurred.",
    "requestId": "0HN2...",
    "details": {
      "document": ["Document is invalid."],
      "coverageEnd": ["CoverageEnd must be greater than CoverageStart."]
    }
  }
}
```

### Success (201)

```json
{
  "id": "b7f2e6f1-2f0d-4d9b-a6b9-11c6b2c6d1a3",
  "number": "SEG-2026-0001",
  "document": "52998224725",
  "licensePlate": "ABC1234",
  "premiumAmount": 199.90,
  "coverageStart": "2026-07-01",
  "coverageEnd": "2027-06-30",
  "status": "Ativa",
  "createdAt": "2026-07-01T14:23:11.123Z",
  "updatedAt": "2026-07-01T14:23:11.123Z"
}
```

Header adicional: `Location: /api/v1/policies/b7f2e6f1-2f0d-4d9b-a6b9-11c6b2c6d1a3`.

---

## 2. GET /api/v1/policies

Lista todas as apólices.

### Query params

- `page` (opcional, default `1`, >= 1)
- `pageSize` (opcional, default `20`, 1..100)

### Success (200)

```json
{
  "data": [ { /* PolicyResponse */ } ],
  "meta": {
    "page": 1,
    "pageSize": 20,
    "total": 42,
    "totalPages": 3
  }
}
```

Ordenação padrão: `createdAt DESC`.

---

## 3. GET /api/v1/policies/{id}

Busca por id.

### Path param

- `id` — Guid

### Responses

- `200 OK` — retorna `PolicyResponse`
- `404 NOT_FOUND` — apólice não existe

---

## 4. PUT /api/v1/policies/{id}

Atualiza uma apólice. Substituição total do lado editável.

### Request

```json
{
  "document": "529.982.247-25",
  "licensePlate": "ABC1D23",
  "premiumAmount": 249.90,
  "coverageStart": "2026-07-01",
  "coverageEnd": "2027-12-31",
  "status": "Ativa"
}
```

- Todos os campos obrigatórios.
- `status` aqui é aceito para permitir `Ativa → Cancelada` ou `Ativa → Expirada`.
- Transição inválida → 422.

### Responses

- `200 OK` — retorna `PolicyResponse` atualizado
- `404 NOT_FOUND` — id não existe
- `400 VALIDATION_ERROR` — validação de payload
- `422 INVALID_STATE` — transição de status proibida

---

## 5. DELETE /api/v1/policies/{id}

Remoção física.

### Responses

- `204 NO_CONTENT`
- `404 NOT_FOUND`

---

## 6. GET /api/v1/policies/expiring

**Requisito exclusivo do PDF.** Lista apólices `Ativa` cuja `coverageEnd` está dentro de `[hoje, hoje+30d]`.

Implementação **obrigatoriamente via SQL cru** (`FromSqlRaw` no repositório).

### Query params

- Nenhum (a janela de 30 dias é fixa por requisito). Configurável apenas via `Segfy:ExpiringWindowDays` no `appsettings.json`.

### Success (200)

```json
{
  "data": [
    {
      "id": "...",
      "number": "SEG-2026-0001",
      "document": "52998224725",
      "licensePlate": "ABC1234",
      "premiumAmount": 199.90,
      "coverageStart": "2026-06-01",
      "coverageEnd": "2026-07-15",
      "status": "Ativa",
      "createdAt": "...",
      "updatedAt": "..."
    }
  ],
  "meta": {
    "windowDays": 30,
    "reference": "2026-07-01"
  }
}
```

Ordenação: `coverageEnd ASC`.

Racional: retornar `meta.windowDays` e `meta.reference` explica ao cliente o que foi consultado — bom para debugging e prova ao avaliador que o número 30 veio de configuração, não de mágica.

---

## 7. GET /health

Health check simples.

### Success (200)

```json
{ "status": "Healthy" }
```

Se algo falhar (ex.: DB inacessível), retorna `503` com `Unhealthy`.

---

## 8. Códigos de erro

| Code | HTTP | Quando |
|---|---|---|
| `VALIDATION_ERROR` | 400 | FluentValidation ou VO de domínio rejeitou input |
| `DOMAIN_VALIDATION` | 400 | `DomainValidationException` (invariante de VO/aggregate) |
| `NOT_FOUND` | 404 | Apólice inexistente |
| `INVALID_STATE` | 422 | Transição de status proibida |
| `INTERNAL_ERROR` | 500 | Fallback para exceções não previstas |

## 9. Content types

- Request: `application/json`
- Response de sucesso: `application/json`
- Response de erro: `application/json`
- Datas: ISO 8601. `date` para vigência (sem hora); `date-time` UTC para timestamps.

## 10. Versionamento

- Prefixo `/api/v1`. Alterações breaking exigem `/api/v2`. Fora de escopo desta entrega.
