# FAQ da Segfy Policies API

Perguntas frequentes sobre o projeto. Serve tanto para quem é da área técnica
quanto para quem não é. Cada resposta começa com uma explicação simples e,
quando faz sentido, tem um bloco extra com o detalhe técnico.

## Sumário

1. [Sobre o projeto](#1-sobre-o-projeto)
2. [Tecnologias escolhidas](#2-tecnologias-escolhidas)
3. [Como a API está organizada](#3-como-a-api-está-organizada)
4. [Conceitos do domínio de apólices](#4-conceitos-do-domínio-de-apólices)
5. [Value Objects, Aggregate Root e outros termos](#5-value-objects-aggregate-root-e-outros-termos)
6. [Contratos, DTOs e Presenters](#6-contratos-dtos-e-presenters)
7. [Regras de negócio](#7-regras-de-negócio)
8. [Erros e validações](#8-erros-e-validações)
9. [Testes](#9-testes)
10. [Deploy e produção](#10-deploy-e-produção)
11. [Coisas que decidimos NÃO usar](#11-coisas-que-decidimos-não-usar)

## 1. Sobre o projeto

### O que é essa API?

É uma API REST que gerencia apólices de seguro de carro. Ela permite cadastrar,
consultar, atualizar e cancelar apólices. Também mostra quais apólices estão
vencendo nos próximos 30 dias.

### Por que ela foi criada?

Como resposta ao desafio técnico Segfy Hands-on Back-end JR. O desafio pede uma
Web API em C# com CRUD, número de apólice gerado no padrão `SEG-YYYY-XXXX`,
persistência em SQL Server ou SQLite, e uma consulta SQL específica para
apólices vencendo em 30 dias.

### Quem vai usar essa API?

Neste teste técnico, o próprio recrutador. Ele abre o Swagger em `/docs` e
consegue clicar em cada endpoint para ver como responde. Não precisa de front
end, o Swagger UI já cumpre esse papel.

### É uma API pronta para produção?

Não exatamente. Foi feita como demonstração técnica, então tem tudo o que se
espera de uma API bem construída (arquitetura em camadas, testes, validação,
logs estruturados), mas o banco escolhido (SQLite) só funciona bem em uma
única instância. Para escalar horizontalmente seria preciso trocar por
PostgreSQL ou SQL Server.

## 2. Tecnologias escolhidas

### Por que C# e .NET 8?

O PDF pede C# com .NET 6 ou superior. Escolhemos .NET 8 porque é a versão LTS
mais recente (com suporte oficial da Microsoft por mais tempo) e traz melhorias
de performance e sintaxe.

### Por que SQLite e não SQL Server?

O PDF permite os dois. Escolhemos SQLite porque:

* Zero configuração. Não precisa instalar nada extra.
* O PDF diz "não queremos instalar mil coisas". SQLite atende isso.
* Para um projeto de 3 dias e volume de teste, funciona perfeitamente.
* Todo o código de acesso a dados fica atrás de uma interface
  (`IPolicyRepository`), então trocar para SQL Server no futuro exige mudar
  apenas a implementação, não o domínio.

### Por que Entity Framework Core?

Ele acelera muito o desenvolvimento comparado a escrever SQL manual.
Detalhe importante: a consulta obrigatória do PDF (apólices vencendo em 30 dias)
usa **SQL puro** via `FromSqlRaw`, respeitando ao pé da letra o que o PDF pede
("implemente uma consulta SQL").

### Preciso instalar alguma coisa para rodar?

Só o **.NET 8 SDK**. Nada mais. Depois de instalado, três comandos e a API sobe.
Também tem Docker configurado, caso prefira.

### Por que Serilog em vez do log padrão do .NET?

Serilog gera logs estruturados em JSON. Cada request tem um `RequestId` único
que aparece tanto no log quanto na resposta de erro, facilitando o
rastreamento de problemas.

### Por que FluentValidation?

Ele separa a validação da regra de negócio. O `CreatePolicyRequestValidator`
checa apenas o formato do payload (campo vazio, número negativo, data
faltando). A validação de regra de negócio (CPF válido, placa no formato
correto) fica nos Value Objects do domínio. Isso evita que a mesma regra
apareça em vários lugares.

## 3. Como a API está organizada

### O que são "camadas"?

Divisões do código por responsabilidade. Cada camada só conhece as camadas
abaixo dela. Isso evita que uma mudança em um lugar quebre coisas em outro.

### Quais camadas existem aqui?

Quatro camadas, cada uma em um projeto separado:

| Camada | Pasta | O que faz |
|---|---|---|
| Domain | `src/Segfy.Domain` | Regras de negócio puras. Não conhece banco, HTTP, nem framework nenhum. |
| Application | `src/Segfy.Application` | Casos de uso (uma classe por operação). Orquestra o domínio. |
| Infrastructure | `src/Segfy.Infrastructure` | Implementa acesso a banco, geração de número, relógio. |
| Api | `src/Segfy.Api` | Controllers HTTP, validação de payload, tradução para JSON. |

### Por que dividir assim?

Facilita testar cada parte isoladamente. O Domain pode ser testado sem banco.
A Application pode ser testada com fakes em memória. Só a Infrastructure
precisa de banco de verdade.

### Regra de dependência entre camadas

* Api conhece Application e Infrastructure.
* Application conhece só Domain.
* Domain não conhece ninguém (nenhuma biblioteca externa).
* Infrastructure conhece Domain e Application.

Essa regra vem de dentro para fora: o Domain é o núcleo puro, e as outras
camadas dependem dele.

### O que é "Composition Root"?

É onde tudo se conecta. No nosso caso, o `Program.cs` da Api. Ele registra
todas as dependências e monta o pipeline HTTP. Nada mais no código sabe como
as coisas são construídas.

## 4. Conceitos do domínio de apólices

### O que é uma apólice?

O contrato de seguro. No nosso caso, tem:

* Número (gerado automaticamente no padrão `SEG-YYYY-XXXX`).
* CPF ou CNPJ do segurado.
* Placa do veículo.
* Valor do prêmio mensal.
* Data de início e fim da vigência.
* Status (Ativa, Cancelada ou Expirada).

### Por que o número é `SEG-YYYY-XXXX`?

O PDF pediu esse formato específico. `SEG` fixo, ano com 4 dígitos, e um
sequencial que reinicia todo ano começando em `0001`.

### Como o número é gerado sem duplicar?

Temos uma tabela chamada `PolicyNumberSequences` com duas colunas: `Year` e
`LastValue`. Toda vez que uma apólice é cadastrada, abrimos uma transação,
lemos o valor atual do ano, incrementamos e gravamos. A transação garante que
duas requisições simultâneas nunca peguem o mesmo número.

Detalhe: o racional completo dessa decisão está em [`sdd/decisions/adr-001.md`](../sdd/decisions/adr-001.md).

### O que é validação mod-11 em CPF/CNPJ?

É o algoritmo matemático que a Receita Federal usa para verificar se um CPF
ou CNPJ é numericamente válido. Cada CPF/CNPJ tem 2 dígitos verificadores no
final, calculados a partir dos outros dígitos.

Isso significa que a API rejeita documentos inventados. Por exemplo,
`12345678901` não passa. Só passa quem tem os dígitos verificadores corretos,
como `52998224725`.

### Quais formatos de placa a API aceita?

Dois:

1. **Padrão antigo brasileiro**: 3 letras + 4 números. Exemplos: `ABC1234`,
   `ABC-1234`. Se vier com hífen, a API remove.
2. **Padrão Mercosul**: 3 letras + 1 número + 1 letra + 2 números.
   Exemplos: `ABC1D23`.

Em ambos os casos a placa é salva em maiúsculas e sem hífen.

### O que é "vigência"?

O período em que a apólice está valendo. Precisa ter uma data de início e uma
data de fim, e a data de fim precisa ser posterior à de início.

### Quais status uma apólice pode ter?

* **Ativa**: valendo normalmente.
* **Cancelada**: cancelada pelo cliente ou pela seguradora.
* **Expirada**: a data de fim da vigência já passou.

### Quais transições de status são permitidas?

* De Ativa para Cancelada: permitido.
* De Ativa para Expirada: permitido.
* Qualquer outra transição é bloqueada (retorna 422).

Cancelada e Expirada são estados **terminais**. Não voltam para Ativa nem
mudam para o outro terminal.

## 5. Value Objects, Aggregate Root e outros termos

### O que é um Value Object?

É um objeto que representa um valor, sem identidade própria. Dois Value
Objects são iguais se têm o mesmo conteúdo. Exemplo: `Money(199.90)` e
`Money(199.90)` são iguais.

Usamos Value Objects para tipos que precisam de validação: `Document`,
`LicensePlate`, `PolicyNumber`, `Money`, `CoveragePeriod`.

### Por que Value Objects em vez de `string` e `decimal`?

Porque a validação fica em UM lugar. Se `Document` for uma `string`, cada
lugar que recebe um documento precisa validar. Se for um `Document` VO, o
próprio tipo garante que só existe um Document se ele foi validado.

### Por que os Value Objects têm dois métodos: `Create` e `LoadTrusted`?

* `Create` valida antes de construir. Se o valor for inválido, lança
  `DomainValidationException`. É usado quando o valor vem de fora
  (usuário, arquivo, request HTTP).
* `LoadTrusted` constrói direto, sem validar. É usado quando o valor vem do
  banco (onde já sabemos que foi validado quando gravou).

### O que é um Aggregate Root?

É a entidade "principal" de um grupo. No nosso caso, `Policy` é a Aggregate
Root. Tudo que se relaciona com uma apólice (o histórico de status, por
exemplo) só pode ser acessado através dela.

### Por que `Policy` tem construtor privado?

Para forçar o uso das factories `Policy.Create` e `Policy.Load`. `Create` é
para uma apólice nova (gera um Id novo, status Ativa). `Load` é para
reidratar uma apólice existente. Isso garante que uma `Policy` nunca é
criada em estado inválido.

### O que é máquina de estado?

É o conjunto de regras que dizem quais mudanças de status são permitidas.
Está no método `PolicyStatusExtensions.CanTransitionTo`. Se você tentar uma
transição não permitida, o próprio domínio lança um erro.

### O que é `DomainException`?

É a exceção base para erros de negócio. Tem 3 subclasses:

* `DomainValidationException`: um Value Object rejeitou um valor.
  Traduzido em HTTP 400.
* `DomainInvalidStateException`: uma operação foi tentada num estado
  inválido (por exemplo, cancelar uma apólice já cancelada).
  Traduzido em HTTP 422.
* `DomainNotFoundException`: apólice inexistente. Traduzido em HTTP 404.

## 6. Contratos, DTOs e Presenters

### O que é um DTO?

Data Transfer Object. Um objeto que serve só para carregar dados de um lugar
para outro, sem regra de negócio.

### Por que tem `CreatePolicyRequest` E `CreatePolicyInput`?

* `CreatePolicyRequest` é o DTO HTTP. Ele espelha o JSON que chega no POST.
  Vive na camada Api.
* `CreatePolicyInput` é o DTO da Application. É o que o caso de uso recebe.
  Vive na camada Application.

Parece duplicação, mas isso desacopla a Api da Application. Se amanhã a
gente quiser adicionar um campo só no HTTP (por exemplo, um cabeçalho de
metadata) sem impactar o caso de uso, dá para fazer.

### O que é um Presenter?

Uma classe estática que transforma uma entidade do domínio em um DTO de
resposta HTTP. No nosso caso, `PolicyPresenter.ToResponse` recebe uma
`Policy` (domínio) e devolve um `PolicyResponse` (HTTP).

O objetivo é: o controller nunca precisa saber como formatar os campos.

### Por que o presenter está separado do controller?

Para o controller focar em 3 coisas: validar payload, chamar o caso de uso,
retornar HTTP. A formatação de resposta é responsabilidade do presenter.

## 7. Regras de negócio

### Por que uma apólice Cancelada não pode ser atualizada?

Porque no mundo real uma apólice cancelada é imutável. Mudar dados de uma
apólice já cancelada não faz sentido no negócio. Regra implementada em
`Policy.UpdateDetails`, que lança `DomainInvalidStateException` se o status
não for Ativa.

### Por que só uma apólice Ativa por placa?

Um carro só pode ter uma seguradora ativa por vez. Se o usuário tenta criar
uma segunda apólice Ativa para uma placa que já tem uma, a API bloqueia.
Regra implementada em `CreatePolicyUseCase` e `UpdatePolicyUseCase`, que
chamam `IPolicyRepository.ExistsActiveByPlateAsync` antes de gravar.

### O que é auto-expiração?

Um serviço que roda em background (`PolicyExpirationHostedService`) e, a cada
hora (configurável), verifica se existe alguma apólice Ativa com
`CoverageEnd < hoje`. Se existir, muda o status para Expirada
automaticamente e registra a mudança no histórico com o motivo
"Auto-expired: coverage period ended.".

### O que é o histórico de status?

Toda vez que o status de uma apólice muda, criamos uma linha na tabela
`PolicyStatusHistory` com:

* Status anterior.
* Status novo.
* Motivo (opcional, veio do usuário ou do sistema).
* Data e hora da mudança.

Consultável em `GET /policies/{id}/history`.

### Por que o motivo é opcional?

Porque nem toda mudança precisa de motivo. Um cancelamento pode ter motivo
(cliente pediu, inadimplência), mas uma expiração automática já explica
sozinha.

### O que acontece se eu tentar criar uma apólice com uma placa que já teve outra apólice, mas essa outra está Cancelada?

Funciona. A regra é "só uma Ativa por placa". Se a outra estiver Cancelada
ou Expirada, você pode criar uma nova Ativa para a mesma placa.

## 8. Erros e validações

### Como é o formato de erro?

Todos os erros da API seguem o mesmo shape:

```json
{
  "error": {
    "code": "VALIDATION_ERROR",
    "message": "One or more validation errors occurred.",
    "requestId": "0HN2XYZ",
    "details": { "document": ["Document is invalid."] }
  }
}
```

### Qual a diferença entre os códigos de erro?

| Código | HTTP | Quando acontece |
|---|---|---|
| `VALIDATION_ERROR` | 400 | Payload malformado ou o FluentValidation rejeitou (campo vazio, tipo errado, JSON quebrado). |
| `DOMAIN_VALIDATION` | 400 | O Value Object rejeitou. Ex: CPF com dígito verificador errado. |
| `NOT_FOUND` | 404 | ID de apólice inexistente. |
| `INVALID_STATE` | 422 | Operação incompatível com o estado atual. Ex: cancelar uma apólice já cancelada. |
| `INTERNAL_ERROR` | 500 | Erro inesperado. A API nunca vaza stack trace no response. |

### Qual a diferença entre VALIDATION_ERROR e DOMAIN_VALIDATION?

`VALIDATION_ERROR` é sobre **forma**: campo faltando, tipo errado, número
negativo. `DOMAIN_VALIDATION` é sobre **semântica**: o formato está certo
mas o valor não faz sentido no negócio (CPF numericamente inválido, placa
que não bate com nenhum padrão brasileiro).

### Por que 422 e não 400 para transição inválida?

422 (Unprocessable Entity) é o código HTTP correto para requests
sintaticamente válidos mas semanticamente incorretos. Se o payload está OK
mas a operação viola uma regra de estado (cancelar algo já cancelado), 422
é mais preciso que 400.

### Por que todo erro tem `requestId`?

Para rastreamento. Se o usuário reporta um problema, o `requestId` no
response bate com o `RequestId` do log estruturado do Serilog. Assim dá para
achar exatamente o que aconteceu.

## 9. Testes

### Por que testes unitários?

O PDF pede. Além disso, testes servem como documentação viva: cada teste
mostra um caso de uso do código.

### Quantos testes têm e onde estão?

**73 testes** no total, divididos em:

* `tests/Segfy.Domain.Tests`: 51 testes que cobrem Value Objects e
  invariantes do aggregate `Policy`.
* `tests/Segfy.Application.Tests`: 22 testes que cobrem cada caso de uso.

Rodam em menos de 100 ms.

### Por que não têm testes de integração?

Para um teste técnico de 3 dias, testes unitários rápidos entregam mais
valor. Testes de integração (subir uma API real e um banco real) tomariam
mais tempo e dariam retorno diminutivo. Se o projeto crescer, faz sentido
adicionar `WebApplicationFactory` para testes end to end.

### O que é um "fake in-memory"?

Uma classe que implementa a mesma interface do repositório real, mas guarda
os dados em uma lista dentro da memória. Assim, os testes de caso de uso
rodam sem tocar em banco. Fica em `tests/Segfy.Application.Tests/Fakes/`.

### Por que não usar Moq (biblioteca de mock)?

Poderíamos, mas para fakes pequenos e comportamentais como os nossos, uma
classe manual é mais legível e mais fácil de dar manutenção.

## 10. Deploy e produção

### Onde posso hospedar essa API?

O jeito mais rápido é o **Render**. Já tem um `render.yaml` no repo que
descreve tudo. Basta apontar o Render para o repositório GitHub e ele faz
o resto.

Outras opções: Fly.io, Railway, Azure App Service, VPS clássica.

### E se o SQLite ficar cheio?

Neste MVP, difícil acontecer. O SQLite aguenta milhões de linhas
tranquilamente para um cenário de leitura/escrita moderado. Para escalar de
verdade (múltiplas instâncias, alta concorrência), trocamos por PostgreSQL
ou SQL Server. Como o `IPolicyRepository` está isolado, a mudança fica
contida na camada Infrastructure.

### Como o auto-expiração se comporta em múltiplas instâncias?

Se rodássemos 2 instâncias do serviço, as duas teriam o background job.
Ambas tentariam expirar as mesmas apólices ao mesmo tempo. Isso funcionaria
por causa da máquina de estado (a segunda tentativa daria `INVALID_STATE`),
mas seria desperdício.

Solução idiomática: usar um lock distribuído (Redis, PostgreSQL advisory
lock) ou uma fila (Hangfire, Quartz). Fora de escopo deste MVP.

### Como aplicar as migrations em produção?

Automaticamente. No `Program.cs` chamamos `Database.Migrate()` no boot, em
qualquer ambiente. Se a flag `Segfy__SeedSampleData` estiver `true` (padrão),
o seeder também popula as 6 apólices de exemplo quando o banco está vazio.

Se preferir controlar manualmente, dá para desativar o `Migrate()` do boot
e rodar `dotnet ef database update` no pipeline de deploy.

### Existe um endpoint de health check?

Sim, `GET /health`. Ele verifica de verdade se o banco SQLite está acessível
(via health check do EF Core): retorna `200` com `{"status":"Healthy", ...}`
quando tudo está bem e `503` quando o banco falha. Usado pelo Docker
`HEALTHCHECK` e pelo Render.

## 11. Coisas que decidimos NÃO usar

O PDF diz "não se empolgue demais". Então evitamos:

### Por que não usar MediatR?

MediatR faz sentido em projetos grandes onde vários casos de uso precisam
ser encadeados ou publicam eventos. Para um CRUD com 6 casos de uso
independentes, adicionar MediatR é over-engineering. Um caso de uso puro
(sem interface, sem handler, sem request), como fizemos, é mais direto e
menos verbose.

### Por que não usar CQRS?

CQRS separa modelos de leitura e escrita. Faz sentido quando a leitura e a
escrita têm padrões muito diferentes (por exemplo, reports complexos com
join massivo). Aqui, o mesmo modelo `Policy` serve para tudo. CQRS traria
mais complexidade sem retorno.

### Por que não usar Event Bus (RabbitMQ / Kafka)?

Event Bus faz sentido para desacoplar sistemas ou processar eventos de
forma assíncrona. A API é síncrona por natureza (CRUD com resposta
imediata). Adicionar Event Bus aqui só criaria pontos extras de falha.

### Por que não usar AutoMapper?

AutoMapper reduz código repetitivo de mapeamento, mas custa em
previsibilidade (mapeamentos "mágicos" difíceis de debugar). Como só temos
2 mapeamentos (Request para Input, Domain para Response), fazer na mão em
um `Presenter` estático é mais claro.

### Por que não usar Docker obrigatório?

O PDF diz "não queremos instalar mil coisas". Se a única maneira de rodar
fosse Docker, o recrutador precisaria instalar o Docker Desktop antes.
Fazemos rodar com só o .NET 8 SDK. Docker é opcional.

### Por que não implementamos autenticação?

O PDF não pede. Adicionar JWT ou OAuth aqui só criaria fricção para o
recrutador testar e não demonstra nada que o PDF valorize.

### Por que não fizemos front end?

O PDF diz "fique à vontade para criar um front end". É opcional. O Swagger
UI em `/docs` já entrega o objetivo (ter um jeito visual de chamar a API)
sem precisar manter mais um projeto.
