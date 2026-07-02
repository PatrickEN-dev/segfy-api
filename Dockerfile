# syntax=docker/dockerfile:1.7

# ---------- Build stage ----------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY Directory.Build.props global.json ./
COPY src/Segfy.Api/Segfy.Api.csproj              src/Segfy.Api/
COPY src/Segfy.Application/Segfy.Application.csproj      src/Segfy.Application/
COPY src/Segfy.Domain/Segfy.Domain.csproj                src/Segfy.Domain/
COPY src/Segfy.Infrastructure/Segfy.Infrastructure.csproj src/Segfy.Infrastructure/

RUN dotnet restore src/Segfy.Api/Segfy.Api.csproj

COPY src/ src/
RUN dotnet publish src/Segfy.Api/Segfy.Api.csproj \
    -c Release \
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

ENV ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_RUNNING_IN_CONTAINER=true \
    DOTNET_gcServer=1 \
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
