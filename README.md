# Segfy Policies API

API REST para cadastro e consulta de apólices de seguro automóvel.
Feita em **C# / .NET 8** para o desafio SegFy Hands-on Back-end JR.

> **Dúvidas sobre como a API foi construída?** Veja o [FAQ em `docs/FAQ.md`](docs/FAQ.md).
> Ele explica em linguagem simples cada decisão (arquitetura, Value Objects,
> Aggregate Root, motivo de cada escolha) para leigos e devs.

---

## Sumário

1. [O que essa API faz](#o-que-essa-api-faz)
2. [Como testar (jeito mais fácil)](#como-testar-jeito-mais-fácil)
3. [Como rodar no seu computador](#como-rodar-no-seu-computador)
4. [O que dá pra fazer na API](#o-que-dá-pra-fazer-na-api)
5. [Regras de negócio em linguagem simples](#regras-de-negócio-em-linguagem-simples)
6. [Requisitos do PDF: onde está cada um](#requisitos-do-pdf-onde-está-cada-um)
7. [Parte técnica (para desenvolvedores)](#parte-técnica-para-desenvolvedores)

---

## O que essa API faz

Ela gerencia **apólices de seguro de carro**. Você pode:

* Cadastrar uma apólice nova (o número da apólice é gerado automaticamente).
* Consultar uma apólice pelo ID, ou ver todas com filtros.
* Atualizar dados de uma apólice.
* Cancelar ou expirar uma apólice.
* Ver quais apólices estão vencendo nos próximos 30 dias.
* Ver o histórico de mudanças de status de uma apólice.

Ao subir, ela já vem com **6 apólices de exemplo** para você testar.

---

## Como testar (jeito mais fácil)

### Opção 1: Testar online no Render (sem instalar nada)

Se você já tem o link do deploy: abra no navegador
`https://<endereço>.onrender.com/docs`

Vai abrir uma tela chamada **Swagger UI**. Nela dá pra clicar em cada endpoint,
apertar "Try it out", preencher os campos e ver a resposta na hora.

> A primeira request depois de 15 min sem uso pode demorar uns 30 segundos
> (o servidor "acorda"). As próximas são instantâneas.

> Prefere Postman ou Insomnia? Tem uma coleção pronta em
> [`docs/`](docs/) com todos os endpoints preenchidos. Veja a
> [Opção 3](#opção-3-importar-a-coleção-pronta-no-postman-ou-insomnia).

### Opção 2: Rodar no seu computador

Veja a seção [Como rodar no seu computador](#como-rodar-no-seu-computador).

### Opção 3: Importar a coleção pronta no Postman ou Insomnia

Se você prefere um cliente de API dedicado, tem uma coleção completa em
[`docs/`](docs/) já com todos os endpoints, bodies de exemplo, filtros
comuns e cenários de erro (400 / 404 / 422). O `policyId` da apólice
recém-criada é gravado automaticamente entre requests, então dá para
executar o fluxo inteiro sem editar nada.

**Arquivos:**

* [`docs/segfy-api.postman_collection.json`](docs/segfy-api.postman_collection.json) — a coleção (formato Postman v2.1, importa nativo no Insomnia).
* [`docs/segfy-api.postman_environment.json`](docs/segfy-api.postman_environment.json) — ambiente **local** (`http://localhost:8080`).
* [`docs/segfy-api.remote.postman_environment.json`](docs/segfy-api.remote.postman_environment.json) — ambiente **Render** (edite `baseUrl` com o link do deploy).

**Como importar:**

* **Insomnia:** `Application` → `Preferences` → `Data` → `Import Data` → `From File`, selecione os 3 arquivos. Depois ative o ambiente no seletor do canto superior.
* **Postman:** botão `Import` no topo, arraste os 3 arquivos. Ative o ambiente no dropdown do canto superior direito.

**Estrutura da coleção:**

| Pasta | O que tem |
|---|---|
| Health | `/health` e `/docs` (Swagger) |
| Fluxo principal | 10 requests numerados: create → get → list → filtros → expiring → update → cancel → history → delete. Rode de cima para baixo. |
| Payloads alternativos | CNPJ + placa Mercosul, apólice vencendo em breve |
| Cenários de erro | Um request para cada código do contrato de erro |

---

## Como rodar no seu computador

### Se você tem o .NET 8 instalado

Só precisa de **três comandos**:

```bash
git clone <link-do-repo>
cd segfy-api
dotnet run --project src/Segfy.Api
```

Abra no navegador:

* **Swagger** (jeito visual de testar): <http://localhost:5000/docs>
* **Health check**: <http://localhost:5000/health>

### Se você prefere Docker

```bash
docker compose up --build
```

Abra: <http://localhost:8080/docs>

### Se você não tem nada instalado

Basta instalar o **.NET 8 SDK** neste link: <https://dotnet.microsoft.com/download/dotnet/8.0>
Depois seguir o passo acima.

---

## O que dá pra fazer na API

Todas as rotas começam com `/api/v1`. Aqui estão elas:

| O que faz | Método | Endereço |
|---|---|---|
| Cadastrar uma apólice | POST | `/policies` |
| Ver todas (com filtros) | GET | `/policies` |
| Ver uma pelo ID | GET | `/policies/{id}` |
| Atualizar uma apólice | PUT | `/policies/{id}` |
| Excluir uma apólice | DELETE | `/policies/{id}` |
| Ver as que vencem em 30 dias | GET | `/policies/expiring` |
| Ver histórico de status | GET | `/policies/{id}/history` |
| Verificar se a API está no ar | GET | `/health` |

### Filtros disponíveis na listagem

Você pode combinar vários parâmetros na URL. Exemplos:

* Só apólices ativas: `/policies?status=Ativa`
* Buscar por CPF que contenha "529": `/policies?document=529`
* Buscar por placa que contenha "ABC": `/policies?licensePlate=ABC`
* Ordenar por vencimento (mais próximo primeiro): `/policies?sortBy=coverageEnd&sortDir=asc`
* Ordenar por prêmio (mais caro primeiro): `/policies?sortBy=premium&sortDir=desc`
* Paginação: `/policies?page=2&pageSize=10`

### Exemplo de cadastro

Cadastrando uma apólice nova:

```json
POST /api/v1/policies

{
  "document": "529.982.247-25",
  "licensePlate": "ABC-1234",
  "premiumAmount": 199.90,
  "coverageStart": "2026-07-05",
  "coverageEnd": "2027-07-04"
}
```

Resposta (201 Created):

```json
{
  "id": "b7f2e6f1-2f0d-4d9b-a6b9-11c6b2c6d1a3",
  "number": "SEG-2026-0007",
  "document": "52998224725",
  "licensePlate": "ABC1234",
  "premiumAmount": 199.90,
  "coverageStart": "2026-07-05",
  "coverageEnd": "2027-07-04",
  "status": "Ativa",
  "createdAt": "2026-07-05T14:23:11.123Z",
  "updatedAt": "2026-07-05T14:23:11.123Z"
}
```

---

## Regras de negócio em linguagem simples

### Como o número da apólice é gerado

Sempre no formato **`SEG-ANO-XXXX`**, por exemplo `SEG-2026-0001`.
O contador reinicia a cada ano.

### Documento (CPF ou CNPJ)

Aceita CPF ou CNPJ, com ou sem máscara. A API valida o dígito verificador
(algoritmo mod-11), então documentos inventados são rejeitados.

### Placa do veículo

Aceita dois formatos:

* Padrão antigo: `ABC-1234` ou `ABC1234`
* Padrão Mercosul: `ABC1D23`

A API normaliza para maiúsculas e sem hífen antes de guardar.

### Valor do prêmio

Precisa ser maior que zero e ter no máximo 2 casas decimais.
Ex: `199.90` funciona, `199.905` é rejeitado.

### Vigência

Sempre no formato `AAAA-MM-DD`. A data de término precisa ser posterior à
data de início.

### Status possíveis

* **Ativa**: apólice funcionando normalmente.
* **Cancelada**: cancelada pelo cliente ou pela seguradora.
* **Expirada**: vigência terminou.

### Transições permitidas

* De **Ativa** para **Cancelada**: OK.
* De **Ativa** para **Expirada**: OK.
* Qualquer outra transição é bloqueada (retorna erro 422).

### Regras extras que criamos

* **Só uma apólice Ativa por placa**. Se você tentar criar uma segunda apólice
  ativa para a mesma placa, a API bloqueia (erro 400).
* **Apólice não pode nascer vencida**. Criar uma apólice com data de término
  anterior a hoje retorna erro 400.
* **Apólice Cancelada ou Expirada é imutável**. Não dá para alterar CPF,
  placa, prêmio ou datas depois disso (erro 422).
* **Histórico de status**. Toda troca de status fica registrada, com data,
  hora e motivo (opcional). Consultável em `/policies/{id}/history`.
* **Auto-expiração**. Uma tarefa em background verifica de tempos em tempos
  se alguma apólice Ativa venceu (`coverageEnd < hoje`) e muda o status para
  Expirada automaticamente.

---

## Requisitos do PDF: onde está cada um

O PDF do desafio pede várias coisas. Aqui está onde cada uma foi feita:

| O que o PDF pede | Onde está | Como confirmar |
|---|---|---|
| Web API com CRUD | `PoliciesController` | Todos os métodos POST/GET/PUT/DELETE estão no Swagger |
| Número no formato `SEG-YYYY-XXXX` | `PolicyNumber.Create` + `SqlitePolicyNumberSequence` | Cadastrar uma apólice e ver o campo `number` |
| CPF/CNPJ validado | Value Object `Document` | Tente cadastrar com `00000000000`, vai dar 400 |
| Placa do veículo | Value Object `LicensePlate` | Tente placa inválida, vai dar 400 |
| Valor do prêmio | Value Object `Money` | Tente valor negativo ou com 3 decimais, vai dar 400 |
| Datas de início e fim | Value Object `CoveragePeriod` | Tente fim menor que início, vai dar 400 |
| Status (Ativa, Cancelada, Expirada) | Enum `PolicyStatus` + máquina de estado | Tente transição inválida, vai dar 422 |
| Banco de dados | SQLite + Entity Framework Core | Arquivo `segfy.db` criado no boot |
| **Consulta SQL das apólices vencendo em 30 dias** | `PolicyRepository.ListExpiringAsync` com `FromSqlRaw` | Endpoint `GET /policies/expiring` |
| Front-end (opcional) | Swagger UI substitui | Acesse `/docs` |
| Testes unitários | `tests/Segfy.Domain.Tests` + `tests/Segfy.Application.Tests` + `tests/Segfy.Api.IntegrationTests` | Rode `dotnet test`, 92 testes |
| README claro | Este arquivo | Você está lendo |

### Coisas extras (além do que o PDF pede)

Feitas para mostrar domínio de arquitetura:

* Arquitetura em camadas isoladas (Domain / Application / Infrastructure / Api).
* Value Objects imutáveis com validação embutida.
* Máquina de estado no `Policy`, sem setters públicos.
* Middleware global de erro com formato JSON padronizado.
* Logs estruturados em JSON (Serilog).
* Configuração validada no boot (`ValidateOnStart`).
* Histórico completo de mudanças de status.
* Background job de auto-expiração.
* Docker + docker-compose + Blueprint para Render prontos.

---

## Parte técnica (para desenvolvedores)

### Stack

* ASP.NET Core 8 (Controllers)
* Entity Framework Core 8 + SQLite
* Serilog (logs em JSON)
* FluentValidation (validação de payload)
* Swashbuckle (Swagger UI)
* xUnit + FluentAssertions (testes)

### Estrutura de pastas

```
src/
  Segfy.Api             Controllers, contratos HTTP, middleware, background jobs
  Segfy.Application     Casos de uso, portas (interfaces), opções
  Segfy.Domain          Aggregate Policy, Value Objects, máquina de estado
  Segfy.Infrastructure  DbContext, repositories, migrations, seed
tests/
  Segfy.Domain.Tests
  Segfy.Application.Tests
  Segfy.Api.IntegrationTests
```

Regras de dependência:

* `Api` depende de `Application` e `Infrastructure`.
* `Application` depende só de `Domain`.
* `Domain` não depende de nada (sem `PackageReference`).
* `Infrastructure` depende de `Application` e `Domain`.

### Como rodar os testes

```bash
dotnet test
```

São 92 testes (52 no Domain, 29 no Application, 11 de integração HTTP).
Os unitários rodam em menos de 100 ms; a suíte completa em poucos segundos.

### Padrões usados

* Aggregate Root, Value Object, Repository
* Use Case (uma classe por operação)
* State Machine no domínio
* Options Pattern + ValidateOnStart
* Structured Logging (Serilog)
* Global Exception Middleware
* Presenter estático para transformar aggregate em DTO de resposta

### Padrões evitados (por escolha)

O PDF pede para não se empolgar. Então **não usamos**:
MediatR, CQRS, Event Bus, AutoMapper, Hangfire, Auth (JWT/OAuth).

### Configuração

Via `appsettings.json` ou variáveis de ambiente (padrão `Segfy__*`):

```json
{
  "Segfy": {
    "ExpiringWindowDays": 30,
    "AutoExpirationEnabled": true,
    "AutoExpirationIntervalSeconds": 3600
  },
  "ConnectionStrings": { "Default": "Data Source=segfy.db;Cache=Shared" }
}
```

A app **quebra no boot** se algum desses valores estiver inválido
(`Options + ValidateOnStart`).

Veja [`.env.example`](.env.example) para uso com Docker.

### Formato de erro (todos os erros seguem esse padrão)

```json
{
  "error": {
    "code": "VALIDATION_ERROR",
    "message": "One or more validation errors occurred.",
    "requestId": "0HN2...",
    "details": { "document": ["Document is invalid."] }
  }
}
```

Códigos possíveis:

| Código | HTTP | Quando acontece |
|---|---|---|
| `VALIDATION_ERROR` | 400 | Payload inválido (campo faltando, tipo errado, regra do FluentValidation) |
| `DOMAIN_VALIDATION` | 400 | Value Object rejeitou (CPF inválido, placa inválida, etc.) |
| `NOT_FOUND` | 404 | Apólice inexistente |
| `INVALID_STATE` | 422 | Transição de status ou update em apólice terminal |
| `INTERNAL_ERROR` | 500 | Erro não previsto (nunca vaza stack trace) |

### Deploy no Render (recomendado)

O repo já vem com [`render.yaml`](render.yaml). Passo a passo:

1. Faça um fork ou push do repo no seu GitHub.
2. No [Render Dashboard](https://dashboard.render.com/), clique em **New +** → **Blueprint**.
3. Selecione o repo.
4. Confirme (o Render lê o `render.yaml`, builda o Dockerfile, aplica migrations e sobe).
5. Em ~3 min, acesse `https://<nome-do-serviço>.onrender.com/docs`.

Detalhes sobre o Free tier do Render:

* Sem custo.
* O serviço dorme após 15 min sem tráfego. Primeira request pós-sleep leva ~30 s.
* Sem disco persistente (disco custa a partir do plano Starter). O SQLite fica
  na rootFS do container e é reprovisionado a cada deploy. O seeder recria as
  6 apólices de exemplo em todo boot.
* Para dados persistentes, aumente para Starter ($7/mês) e descomente o bloco
  `disk` no `render.yaml`.

### Outras opções de deploy

| Alvo | Free tier? | Ideal para |
|---|---|---|
| Render | Sim (Web) | Este teste técnico (Blueprint pronto) |
| Fly.io | Sim | `fly launch` lê o Dockerfile; LiteFS pra SQLite persistente |
| Railway | Trial | GitHub push, Docker, volume |
| Azure App Service (Linux) | 60 dias trial | .NET nativo; SQLite em `/home/site/wwwroot` |
| VPS (DigitalOcean, Hetzner) | Não | `dotnet publish` + systemd |

Para produção com múltiplas instâncias, SQLite vira gargalo. Troque a
connection string para PostgreSQL ou SQL Server. O `IPolicyRepository` está
isolado; só `SqlitePolicyNumberSequence` precisaria de uma variante.

### Reset do banco (dev)

```bash
dotnet tool install --global dotnet-ef
dotnet ef database drop -f --project src/Segfy.Infrastructure --startup-project src/Segfy.Api
dotnet run --project src/Segfy.Api
```

### Decisões arquiteturais

Detalhes completos vivem em [`sdd/`](sdd/) (pacote Spec-Driven Development).
O ADR do gerador atômico de número da apólice está em
[`sdd/decisions/adr-001.md`](sdd/decisions/adr-001.md).
