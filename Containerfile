# syntax=docker/dockerfile:1

# ─────────────────────────────────────────────────────────────────────────────
# Build stage — full .NET 10 SDK. Nothing here ends up on the Fedora host.
# We restore/publish ONLY the Virginia web project, so Virginia.AppHost (Aspire
# orchestrator) is never built and the Aspire workload is never required.
# ─────────────────────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution-wide MSBuild config first so layer caching works for restore.
COPY Directory.Build.props Directory.Packages.props ./

# Copy the projects the web app actually depends on.
COPY Virginia.ServiceDefaults/ Virginia.ServiceDefaults/
COPY Virginia/ Virginia/

# Restore + publish the web project only.
RUN dotnet restore Virginia/Virginia.csproj
RUN dotnet publish Virginia/Virginia.csproj \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

# ─────────────────────────────────────────────────────────────────────────────
# Runtime stage — ASP.NET runtime only. No SDK, no source.
# ─────────────────────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Kestrel listens on 8080 (plain HTTP); a proxy/host maps it outward.
ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_EnableDiagnostics=0

COPY --from=build /app/publish ./

# Persisted data lives here; mounted as a volume in compose.
RUN mkdir -p /data && chown -R app:app /data /app
VOLUME /data

USER app
EXPOSE 8080

ENTRYPOINT ["dotnet", "Virginia.dll"]
