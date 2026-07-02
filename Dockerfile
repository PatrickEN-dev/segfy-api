# syntax=docker/dockerfile:1.7

# ---------- Build stage ----------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and props first — restore layer caches unless a project file changes.
COPY Directory.Build.props global.json ./
COPY src/Segfy.Api/Segfy.Api.csproj                       src/Segfy.Api/
COPY src/Segfy.Application/Segfy.Application.csproj       src/Segfy.Application/
COPY src/Segfy.Domain/Segfy.Domain.csproj                 src/Segfy.Domain/
COPY src/Segfy.Infrastructure/Segfy.Infrastructure.csproj src/Segfy.Infrastructure/

# BuildKit cache mount: NuGet packages persist across builds. Local rebuilds skip
# re-download; CI benefits alongside the GHA layer cache. -r linux-x64 pulls the
# runtime package needed by PublishReadyToRun (declared in Segfy.Api.csproj).
RUN --mount=type=cache,id=nuget,target=/root/.nuget/packages \
    dotnet restore src/Segfy.Api/Segfy.Api.csproj -r linux-x64

COPY src/ src/

# ReadyToRun: precompiled native code for hot methods. Cuts cold-start ~20-40%,
# critical for free-tier hosts (Render/Fly) that sleep the instance.
# --no-self-contained keeps the app framework-dependent (runtime image already
# ships ASP.NET); otherwise -r would produce a self-contained bundle (~80 MB extra).
RUN --mount=type=cache,id=nuget,target=/root/.nuget/packages \
    dotnet publish src/Segfy.Api/Segfy.Api.csproj \
    -c Release \
    -r linux-x64 \
    --no-self-contained \
    -o /app/publish \
    /p:UseAppHost=false \
    --no-restore

# ---------- Runtime stage ----------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# curl is needed for HEALTHCHECK; base image (bookworm-slim) doesn't ship it.
# Non-root user (defense in depth).
RUN apt-get update \
 && apt-get install -y --no-install-recommends curl \
 && rm -rf /var/lib/apt/lists/* \
 && groupadd --system --gid 1001 segfy \
 && useradd  --system --uid 1001 --gid segfy --home /app segfy \
 && mkdir -p /app/data \
 && chown -R segfy:segfy /app

COPY --from=build --chown=segfy:segfy /app/publish ./

VOLUME ["/app/data"]

# DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1 drops ICU (~30 MB) since our code uses
# InvariantCulture everywhere. Do NOT enable if you introduce locale-sensitive
# parsing/formatting elsewhere.
ENV ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_RUNNING_IN_CONTAINER=true \
    DOTNET_gcServer=1 \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1 \
    ConnectionStrings__Default="Data Source=/app/data/segfy.db;Cache=Shared" \
    Segfy__ExpiringWindowDays=30 \
    Segfy__AutoExpirationEnabled=true \
    Segfy__AutoExpirationIntervalSeconds=3600

USER segfy
EXPOSE 8080

# Render/Fly/Railway inject $PORT at runtime; fall back to 8080 locally.
HEALTHCHECK --interval=30s --timeout=5s --start-period=15s --retries=3 \
  CMD curl -fsS "http://127.0.0.1:${PORT:-8080}/health" || exit 1

ENTRYPOINT ["/bin/sh", "-c", "exec dotnet Segfy.Api.dll --urls http://+:${PORT:-8080}"]
