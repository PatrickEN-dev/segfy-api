# Coding Guidelines

Aplicam-se a todos os projetos. Não são sugestões.

## Formatação

- `.editorconfig` na raiz com padrões `dotnet format`.
- 4 espaços; LF; UTF-8 sem BOM.
- Uma classe pública por arquivo; nome do arquivo bate com a classe.

## Nullable & warnings

- `<Nullable>enable</Nullable>` em todo csproj.
- `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` em todo csproj.
- `!` (null-forgiving) só quando comprovadamente seguro, com comentário curto explicando.
- Sem `#pragma warning disable` ou `#nullable disable` sem justificativa em comentário.

## Naming

| Elemento | Convenção | Exemplo |
|---|---|---|
| Namespace | `Segfy.<Camada>.<Contexto>` | `Segfy.Domain.Policies.ValueObjects` |
| Classe/interface | `PascalCase`; interface prefixo `I` | `IPolicyRepository` |
| Método | `PascalCase`; async com sufixo `Async` | `ExecuteAsync` |
| Parâmetro/local | `camelCase` | `policyNumber` |
| Campo privado | `_camelCase` | `_repo` |
| Constante | `PascalCase` | `DefaultExpiringWindowDays` |
| Enum | singular | `PolicyStatus` |
| Value Object | nome direto, sem sufixo `VO` | `Money`, `Document` |
| Use Case | `<Verbo><Substantivo>UseCase` | `CreatePolicyUseCase` |
| Request DTO | `<Verbo><Substantivo>Request` | `CreatePolicyRequest` |
| Response DTO | `<Substantivo>Response` | `PolicyResponse` |
| Application input | `<Verbo><Substantivo>Input` | `CreatePolicyInput` |
| Exception | `<Motivo>Exception` | `DomainNotFoundException` |

## Idioma

- **Domínio em português**: enum `PolicyStatus { Ativa, Cancelada, Expirada }`. Preserva termo de negócio.
- **Código em inglês**: nomes de classe, método, variável, log, mensagem de erro cliente.
- Logs em inglês. Sempre.

## Records vs classes

- `record` para DTOs, VOs, Input/Output de use cases.
- `sealed class` para entities, use cases, services, controllers.
- `sealed` por padrão em qualquer classe não projetada para herança.

## Async

- I/O = `async`. Sufixo `Async` obrigatório.
- Passar `CancellationToken` em todos os métodos async públicos com I/O.
- **Proibido**: `.Result`, `.Wait()`, `.GetAwaiter().GetResult()`, `async void` (fora de bootstrap).
- Não usar `ConfigureAwait(false)` — desnecessário em ASP.NET Core 8+.

## Dependency Injection

- Cada camada expõe `AddXxx(this IServiceCollection)` em `DependencyInjection.cs`.
- Ordem no `Program.cs`: `AddApplication → AddInfrastructure(cfg) → AddApiServices(cfg)`.
- Injeção por construtor (ou primary constructor). **Proibido** service locator.
- Lifetime padrão: `Scoped`. `Singleton` só para stateless comprovado (`SystemClock`).

## Validação (duas camadas, sem sobreposição)

| Onde | O que valida |
|---|---|
| FluentValidation em `Api/Validators` | `NotEmpty`, `MaximumLength`, `GreaterThan`, comparação entre campos |
| Value Object em `Domain/.../ValueObjects` | Mod-11, regex de placa, formato do número, invariantes de valor |

Não duplique. Se o VO já valida semanticamente, o FV **não** repete.

## Exceções

- Todas de negócio derivam de `DomainException`.
- Subclasses: `DomainValidationException` (400), `DomainInvalidStateException` (422), `DomainNotFoundException` (404).
- Cada uma expõe `Code : string` upper-snake.
- **Proibido**: `throw new ArgumentException(...)` ou `InvalidOperationException(...)` para representar regra de negócio.
- Nunca `throw ex;` (perde stack). Use `throw;` para rethrow.
- Nunca `catch (Exception) { }` silencioso.

## Value Objects

- `sealed record` com construtor privado.
- Factory `Create(...)` valida e constrói. Factory `LoadTrusted(...)` reidrata sem validar (usado pelo EF value converter).
- `Equals` e `GetHashCode` gerados pelo record.
- Falha de `Create` → `DomainValidationException`.

## Aggregate

- `Policy` é `sealed class` (tem comportamento).
- Construtor privado. Factories públicas: `Create` (novo) e `Load` (reidratação do banco).
- Sem setters públicos. Mutação via métodos explícitos: `UpdateDetails`, `ChangeStatus`.

## Repositórios

- Interface no `Segfy.Domain.<Contexto>.Abstractions`.
- Impl em `Segfy.Infrastructure.Persistence.Repositories`.
- Cada método chama `SaveChangesAsync` internamente (Unit of Work implícito).
- Retorna sempre aggregate. Nunca DTO.

## Use Cases

- Um método público `ExecuteAsync`.
- Retorna aggregate ou `Task`.
- Depende só de abstrações (`IPolicyRepository`, `IClock`, `IPolicyNumberSequence`).
- Levanta apenas `DomainException` e derivadas.

## Controllers

- `[ApiController]` + `[Route("api/v1/[controller]")]`.
- 3 responsabilidades: validar (via `IValidator<T>`), delegar (`await useCase.ExecuteAsync(...)`), traduzir (via `Presenter`).
- Nunca chama `DbContext` ou repositório direto.

## Presenters

- `public static class` sem estado.
- Um método por tipo de resposta: `ToResponse(Policy p) => new PolicyResponse(...)`.

## Testes

- Nome: `MethodUnderTest_Scenario_ExpectedResult`.
- Estrutura AAA sem comentários separando.
- FluentAssertions: `result.Should().Be(...)`.
- Sem Moq quando um fake in-memory à mão resolve.
- Tempo total dos testes < 5s.

## Logs

- `ILogger<T>` injetado.
- Estruturado: `_logger.LogInformation("Created policy {PolicyNumber}", number.Value);` (nunca string interpolation direta).
- **Nunca** logar `Document` puro. Se necessário, mascarar.
- Níveis: `Information` (start/end/created), `Warning` (4xx cliente), `Error` (5xx / exceção não prevista).

## Configuração

- Nada de `Environment.GetEnvironmentVariable` em código de negócio. Via `IOptions<T>`.
- Secrets nunca em `appsettings.json` versionado.

## Anti-padrões proibidos

- Retornar `null` sem `T?` explícito.
- Injetar `IServiceProvider` num handler.
- `catch (Exception) { }` silencioso.
- `throw ex;`.
- `#region`.
- Métodos > 30 linhas.
- Classes > 300 linhas.
- Static mutable state.
- Reflection em runtime path (fora de DI/bootstrap).
