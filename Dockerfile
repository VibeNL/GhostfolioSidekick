# Use specific platform and optimize base images
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:9.0 AS build

ARG TARGETARCH
ARG BUILDPLATFORM
ARG TARGETPLATFORM

WORKDIR /src

# Install dependencies in optimized layers
RUN apt-get update && \
    apt-get install -y --no-install-recommends \
        python3 \
        python3-pip \
        supervisor \
        curl && \
    curl -fsSL https://deb.nodesource.com/setup_20.x | bash - && \
    apt-get install -y --no-install-recommends nodejs && \
    dotnet workload install wasm-tools && \
    apt-get clean && \
    rm -rf /var/lib/apt/lists/* /tmp/* /var/tmp/*

# Copy project files in optimal order for caching
COPY ["PortfolioViewer/PortfolioViewer.ServiceDefaults/PortfolioViewer.ServiceDefaults.csproj", "PortfolioViewer/PortfolioViewer.ServiceDefaults/"]
COPY ["PortfolioViewer/PortfolioViewer.Common/PortfolioViewer.Common.csproj", "PortfolioViewer/PortfolioViewer.Common/"]
COPY ["Database/Database.csproj", "Database/"]
COPY ["Model/Model.csproj", "Model/"]
COPY ["Configuration/Configuration.csproj", "Configuration/"]
COPY ["GhostfolioAPI/GhostfolioAPI.csproj", "GhostfolioAPI/"]
COPY ["Parsers/Parsers.csproj", "Parsers/"]
COPY ["Cryptocurrency/Cryptocurrency.csproj", "Cryptocurrency/"]
COPY ["ExternalDataProvider/ExternalDataProvider.csproj", "ExternalDataProvider/"]
COPY ["PortfolioViewer/PortfolioViewer.ApiService/PortfolioViewer.ApiService.csproj", "PortfolioViewer/PortfolioViewer.ApiService/"]
COPY ["PortfolioViewer/PortfolioViewer.WASM.AI/PortfolioViewer.WASM.AI.csproj", "PortfolioViewer/PortfolioViewer.WASM.AI/"]
COPY ["PortfolioViewer/PortfolioViewer.WASM/PortfolioViewer.WASM.csproj", "PortfolioViewer/PortfolioViewer.WASM/"]
COPY ["GhostfolioSidekick/GhostfolioSidekick.csproj", "GhostfolioSidekick/"]

# Restore dependencies for all projects
RUN dotnet restore "PortfolioViewer/PortfolioViewer.ApiService/PortfolioViewer.ApiService.csproj" && \
    dotnet restore "PortfolioViewer/PortfolioViewer.WASM/PortfolioViewer.WASM.csproj" && \
    dotnet restore "GhostfolioSidekick/GhostfolioSidekick.csproj"

# Copy the entire source code
COPY . .

# Build all projects in Release mode
# Build each project separately and handle any gRPC protoc issues by using build platform tools
RUN dotnet build "PortfolioViewer/PortfolioViewer.ApiService/PortfolioViewer.ApiService.csproj" \
        -c Release \
        --no-restore \
        /p:ProtobufToolsOs=linux \
        /p:ProtobufToolsCpu=x64 || \
    (echo "First build failed, trying without protobuf arch specification..." && \
     dotnet build "PortfolioViewer/PortfolioViewer.ApiService/PortfolioViewer.ApiService.csproj" \
        -c Release \
        --no-restore)

RUN dotnet build "PortfolioViewer/PortfolioViewer.WASM/PortfolioViewer.WASM.csproj" \
        -c Release \
        --no-restore \
        /p:ProtobufToolsOs=linux \
        /p:ProtobufToolsCpu=x64 || \
    (echo "First WASM build failed, trying without protobuf arch specification..." && \
     dotnet build "PortfolioViewer/PortfolioViewer.WASM/PortfolioViewer.WASM.csproj" \
        -c Release \
        --no-restore)

RUN dotnet build "GhostfolioSidekick/GhostfolioSidekick.csproj" \
        -c Release \
        --no-restore

# Publish API service
FROM build AS publish-api
WORKDIR "/src/PortfolioViewer/PortfolioViewer.ApiService"
RUN dotnet publish "PortfolioViewer.ApiService.csproj" \
    -c Release \
    --no-build \
    --no-restore \
    -o /app/publish \
    /p:UseAppHost=false

# Publish WASM
FROM build AS publish-wasm
WORKDIR "/src/PortfolioViewer/PortfolioViewer.WASM"
RUN dotnet publish "PortfolioViewer.WASM.csproj" \
    -c Release \
    --no-build \
    --no-restore \
    -o /app/publish-wasm \
    /p:UseAppHost=false

# Publish Sidekick
FROM build AS publish-sidekick
WORKDIR "/src/GhostfolioSidekick"
RUN dotnet publish "GhostfolioSidekick.csproj" \
    -c Release \
    --no-build \
    --no-restore \
    -o /app/publish-sidekick \
    /p:UseAppHost=false

# Final runtime image - optimized base
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final

# Create non-root user for security
RUN groupadd -r appgroup && useradd -r -g appgroup appuser

WORKDIR /app

# Install only supervisor in a single optimized layer
RUN apt-get update && \
    apt-get install -y --no-install-recommends supervisor curl && \
    apt-get clean && \
    rm -rf /var/lib/apt/lists/* /tmp/* /var/tmp/*

# Copy published outputs with proper ownership
COPY --from=publish-api --chown=appuser:appgroup /app/publish ./
COPY --from=publish-wasm --chown=appuser:appgroup /app/publish-wasm/wwwroot ./wwwroot
COPY --from=publish-sidekick --chown=appuser:appgroup /app/publish-sidekick ./

# Copy supervisord config
COPY --chown=appuser:appgroup supervisord.conf /etc/supervisor/conf.d/supervisord.conf

# Create directories for certificates and logs
RUN mkdir -p /https /var/log/supervisor && \
    chown -R appuser:appgroup /https /var/log/supervisor /app

# Set environment variables for production
ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_URLS="http://+:80;https://+:443" \
    DOTNET_RUNNING_IN_CONTAINER=true \
    DOTNET_USE_POLLING_FILE_WATCHER=true \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:80/health || exit 1

EXPOSE 80 443

# Switch to non-root user
USER appuser

# Start app via supervisord
CMD ["supervisord", "-c", "/etc/supervisor/conf.d/supervisord.conf"]
