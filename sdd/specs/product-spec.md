# Product Spec — Apólice de Seguro Automóvel

Todos os requisitos abaixo vêm **exclusivamente do PDF** do desafio SegFy. Nada aqui é derivado ou inferido além do explicitamente listado. Assunções complementares vivem em [`../PROJECT_CONTEXT.md`](../PROJECT_CONTEXT.md#assunções-mínimas-documentadas).

---

## 1. Objetivo

Construir uma Web API para **cadastrar e consultar apólices de seguro automóvel**, com operações CRUD, um formato específico de número da apólice, e uma consulta de vencimento.

## 2. Requisitos funcionais

### RF-01 — Cadastro de apólice (Create)

O sistema deve permitir registrar uma apólice de seguro automóvel com os seguintes campos:

| Campo | Origem | Regra |
|---|---|---|
| Número da apólice | **auto-gerado pelo sistema** | Formato `SEG-YYYY-XXXX` |
| CPF ou CNPJ do segurado | input do cliente | Um dos dois; validado |
| Placa do veículo | input do cliente | Padrão brasileiro |
| Valor do prêmio | input do cliente | Valor mensal pago pelo cliente |
| Data de início da vigência | input do cliente | Obrigatória |
| Data de término da vigência | input do cliente | Obrigatória; posterior à de início |
| Status | input do cliente / default | Um de: `Ativa`, `Cancelada`, `Expirada` |

### RF-02 — Consulta unitária (Read by id)

Buscar uma apólice pelo identificador. Retornar `404` se não encontrada.

### RF-03 — Listagem (Read many)

Listar apólices existentes.

### RF-04 — Atualização (Update)

Atualizar dados de uma apólice existente. Retornar `404` se não encontrada.

### RF-05 — Exclusão (Delete)

Excluir uma apólice pelo identificador. Retornar `404` se não encontrada.

### RF-06 — Consulta de apólices vencendo nos próximos 30 dias

Deve existir uma **consulta SQL** (não ORM/LINQ, conforme redação literal do PDF: *"implemente uma consulta SQL que liste as apólices que vencem nos próximos 30 dias"*) que retorne todas as apólices cuja data de término da vigência esteja dentro da janela `[hoje, hoje + 30 dias]`.

### RF-07 — Persistência

Os dados devem ser persistidos em **SQL Server ou SQLite**. Este projeto opta por **SQLite** — decisão registrada em `PROJECT_CONTEXT.md`.

### RF-08 (opcional) — Front-end

O PDF explicita que o front-end é **opcional** ("Fique à vontade para criar um front-end que faça as chamadas da API"). **Este projeto não entrega front-end.** Swagger em `/docs` cobre a necessidade de "chamar a API" para o avaliador.

## 3. Requisitos não-funcionais

| Código | Requisito | Origem |
|---|---|---|
| RNF-01 | Runtime `>= .NET 6` | PDF |
| RNF-02 | Executar sem instalar "mil coisas" | PDF ("#FicaDica") |
| RNF-03 | Testes unitários | PDF ("Gostamos muito de testes unitários") |
| RNF-04 | Código público em repositório GitHub | PDF |
| RNF-05 | `README.md` com instruções claras de execução | PDF |
| RNF-06 | Demonstrar domínio de arquitetura e engenharia de software | PDF |
| RNF-07 | Prazo de 3 dias para execução | PDF |
| RNF-08 | Não exagerar — "não se empolgue demais" | PDF |

## 4. Regras de negócio derivadas dos requisitos

### RN-01 — Formato do número da apólice

- Padrão: `SEG-YYYY-XXXX`
- `SEG` fixo
- `YYYY` = ano de emissão (4 dígitos)
- `XXXX` = sequencial de emissão dentro do ano, zero-padded no mínimo em 4 dígitos, expansível se ultrapassar `9999` (`00001`, `00002`…)
- Único no sistema
- Gerado atomicamente no ato do cadastro

Estratégia de geração em SQLite documentada em [`decisions/adr-001.md`](../decisions/adr-001.md).

### RN-02 — Validação de CPF/CNPJ

- Campo aceita **CPF** (11 dígitos) ou **CNPJ** (14 dígitos)
- Caracteres não numéricos são removidos antes de validar
- Validação de dígito verificador (mod-11) obrigatória
- Formato de armazenamento: apenas dígitos, sem máscara

### RN-03 — Validação de placa

- Aceita padrão antigo `AAA-9999` ou `AAA9999`
- Aceita padrão Mercosul `AAA9A99`
- Normalizada em maiúsculas, sem hífen na persistência

### RN-04 — Valor do prêmio

- Valor monetário em Reais (BRL)
- Deve ser estritamente maior que 0
- Até 2 casas decimais
- Armazenado como `decimal(18,2)`

### RN-05 — Vigência

- `CoverageStart` obrigatório
- `CoverageEnd` obrigatório
- `CoverageEnd > CoverageStart` (invariante)
- Ambas são datas (sem horário)

### RN-06 — Status

- Enum: `Ativa`, `Cancelada`, `Expirada`
- Default de cadastro: `Ativa`
- Transições permitidas:
  - `Ativa → Cancelada`
  - `Ativa → Expirada`
- `Cancelada` e `Expirada` são terminais (não voltam)
- Tentativa de transição inválida lança erro de domínio

### RN-07 — Consulta de vencimento

- Retorna apólices onde:
  - `CoverageEnd BETWEEN CURRENT_DATE AND CURRENT_DATE + 30 dias`
  - `Status = 'Ativa'` (canceladas e expiradas não vencem)
- Ordenação: `CoverageEnd ASC` (as mais próximas primeiro)
- Implementação obrigatoriamente via **SQL cru** (`FromSqlRaw` ou similar)

## 5. Fora de escopo (explicitamente)

Nada abaixo será implementado, pois não está no PDF:

- Autenticação (JWT, OAuth, API Key)
- Autorização por role
- Multi-tenant (broker/corretor)
- Front-end (opcional; será omitido)
- Soft delete
- Auditoria de mudanças
- Notificações
- Cache
- Rate limiting
- Internacionalização
- Webhooks
- Renovação automática de apólice
- Cálculo de prêmio
- Sinistros

## 6. Critérios de aceitação globais

O projeto está aceito quando:

- [ ] Existe um endpoint para cada operação de RF-01 a RF-06.
- [ ] `SEG-YYYY-XXXX` é gerado corretamente para cada cadastro.
- [ ] CPF/CNPJ inválidos são rejeitados com `400`.
- [ ] Placas inválidas são rejeitadas com `400`.
- [ ] `CoverageEnd <= CoverageStart` é rejeitado com `400`.
- [ ] Transição de status inválida é rejeitada com `422`.
- [ ] Get/Update/Delete em id inexistente retornam `404`.
- [ ] `GET /policies/expiring` usa SQL cru e retorna somente `Ativa` com vencimento em <= 30 dias.
- [ ] Swagger em `/docs` documenta todos os endpoints.
- [ ] `README.md` contém 3 comandos ou menos para subir a API.
- [ ] Há testes unitários para VOs e para cada use case.
- [ ] `dotnet build -warnaserror` e `dotnet test` passam.
