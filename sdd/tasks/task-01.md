# Task 01 — Bootstrap da Solution

## Objetivo

Criar estrutura vazia: `.sln`, 4 projetos em `src/`, 2 projetos em `tests/`, `Directory.Build.props`, `.editorconfig`, `.gitignore`. `Program.cs` mínimo responde `/health`. Zero lógica de negócio.

## Prerequisites

- .NET 8 SDK instalado (`dotnet --version` >= 8.0).
- Global tool `dotnet-ef` (usada na task-04):
  ```
  dotnet tool install --global dotnet-ef
  ```
  Verificar: `dotnet ef --version` >= 8.0.

## Files to create

### Raiz

- `Segfy.sln`
- `Directory.Build.props`:
  ```xml
  <Project>
    <PropertyGroup>
      <TargetFramework>net8.0</TargetFramework>
      <LangVersion>latest</LangVersion>
      <Nullable>enable</Nullable>
      <ImplicitUsings>enable</ImplicitUsings>
      <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
      <EnableNETAnalyzers>true</EnableNETAnalyzers>
      <AnalysisLevel>latest-recommended</AnalysisLevel>
    </PropertyGroup>
  </Project>
  ```
- `.editorconfig` — base `dotnet format`, `end_of_line=lf`, `insert_final_newline=true`, `charset=utf-8`, indent=4 spaces em `.cs`.
- `.gitignore` — padrão Visual Studio + `*.db`, `*.db-shm`, `*.db-wal`, `TestResults/`.
- `global.json` (opcional) — fixa SDK 8.0.x.

### Projetos

- `src/Segfy.Domain/` — `classlib`. Sem `PackageReference`. Coloque um `_placeholder.cs` temporário (removido na task-02).
- `src/Segfy.Application/` — `classlib`. Referencia `Segfy.Domain`. `PackageReference`: `Microsoft.Extensions.Options`.
- `src/Segfy.Infrastructure/` — `classlib`. Referencia `Segfy.Domain`, `Segfy.Application`. `PackageReference`: `Microsoft.EntityFrameworkCore`, `Microsoft.EntityFrameworkCore.Sqlite`, `Microsoft.EntityFrameworkCore.Design`.
- `src/Segfy.Api/` — `webapi` (SDK `Microsoft.NET.Sdk.Web`). Referencia `Segfy.Application`, `Segfy.Infrastructure`. `PackageReference`: `Swashbuckle.AspNetCore`, `Serilog.AspNetCore`, `Serilog.Sinks.Console`, `Serilog.Formatting.Compact`, `FluentValidation`, `FluentValidation.DependencyInjectionExtensions`.
- `tests/Segfy.Domain.Tests/` — `xunit`. Referencia `Segfy.Domain`. `PackageReference`: `xunit`, `xunit.runner.visualstudio`, `Microsoft.NET.Test.Sdk`, `FluentAssertions`.
- `tests/Segfy.Application.Tests/` — `xunit`. Referencia `Segfy.Application`, `Segfy.Domain`. Mesmos packages de Domain.Tests.

### `src/Segfy.Api/Program.cs` (mínimo)

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();
app.UseSwagger();
app.UseSwaggerUI(o => o.RoutePrefix = "docs");
app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "Healthy" }));
app.Run();
```

### `src/Segfy.Api/appsettings.json`

```json
{
  "Segfy": { "ExpiringWindowDays": 30 },
  "ConnectionStrings": { "Default": "Data Source=segfy.db;Cache=Shared" },
  "Logging": { "LogLevel": { "Default": "Information" } },
  "AllowedHosts": "*"
}
```

## Files to modify

Nenhum.

## Acceptance criteria

- [ ] `dotnet build Segfy.sln -warnaserror` verde.
- [ ] `dotnet run --project src/Segfy.Api` responde `200 { "status": "Healthy" }` em `GET /health`.
- [ ] `GET /docs` abre Swagger UI (mesmo vazio).
- [ ] `dotnet test` roda com 0 testes sem erro.

## Definition of Done

Ver template em `CLAUDE.md §Definition of Done`.
