# syntax=docker/dockerfile:1
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:9.0 AS build

ARG TARGETARCH
ARG BUILDPLATFORM

# Install dependencies in a single layer for better caching
RUN apt-get update && \
    apt-get install -y --no-install-recommends \
        python3 \
        python3-pip \
        curl \
        ca-certificates && \
    curl -fsSL https://deb.nodesource.com/setup_20.x | bash - && \
    apt-get install -y --no-install-recommends nodejs && \
    dotnet workload install wasm-tools && \
    apt-get clean && \
    rm -rf /var/lib/apt/lists/* /tmp/* /var/tmp/*

WORKDIR /src

# Copy solution file first for better layer caching
COPY *.sln ./

# Copy all project files in a structured way for optimal caching
COPY ["GhostfolioSidekick/GhostfolioSidekick.csproj", "GhostfolioSidekick/"]
COPY ["PortfolioViewer/PortfolioViewer.ApiService/PortfolioViewer.ApiService.csproj", "PortfolioViewer/PortfolioViewer.ApiService/"]
COPY ["PortfolioViewer/PortfolioViewer.WASM/PortfolioViewer.WASM.csproj", "PortfolioViewer/PortfolioViewer.WASM/"]
COPY ["PortfolioViewer/PortfolioViewer.Common/PortfolioViewer.Common.csproj", "PortfolioViewer/PortfolioViewer.Common/"]
COPY ["PortfolioViewer/PortfolioViewer.ServiceDefaults/PortfolioViewer.ServiceDefaults.csproj", "PortfolioViewer/PortfolioViewer.ServiceDefaults/"]
COPY ["PortfolioViewer/PortfolioViewer.WASM.Data/PortfolioViewer.WASM.Data.csproj", "PortfolioViewer/PortfolioViewer.WASM.Data/"]

# Copy supporting library projects
COPY ["Model/Model.csproj", "Model/"]
COPY ["Configuration/Configuration.csproj", "Configuration/"]
COPY ["Database/Database.csproj", "Database/"]
COPY ["GhostfolioAPI/GhostfolioAPI.csproj", "GhostfolioAPI/"]
COPY ["ExternalDataProvider/ExternalDataProvider.csproj", "ExternalDataProvider/"]
COPY ["Parsers/Parsers.csproj", "Parsers/"]
COPY ["Cryptocurrency/Cryptocurrency.csproj", "Cryptocurrency/"]
COPY ["PerformanceCalculations/PerformanceCalculations.csproj", "PerformanceCalculations/"]

# Restore dependencies for the main projects only (dependencies will be resolved)
RUN dotnet restore -a $TARGETARCH "GhostfolioSidekick/GhostfolioSidekick.csproj" && \
    dotnet restore -a $TARGETARCH "PortfolioViewer/PortfolioViewer.ApiService/PortfolioViewer.ApiService.csproj" && \
    dotnet restore -a $TARGETARCH "PortfolioViewer/PortfolioViewer.WASM/PortfolioViewer.WASM.csproj"

# Copy source code
COPY . .

# Build and publish in optimized way
RUN dotnet publish "GhostfolioSidekick/GhostfolioSidekick.csproj" \
        -a $TARGETARCH \
        -c Release \
        --no-restore \
        -p:PublishSingleFile=false \
        -p:PublishTrimmed=false \
        -o /app/publish-sidekick && \
    dotnet publish "PortfolioViewer/PortfolioViewer.ApiService/PortfolioViewer.ApiService.csproj" \
        -a $TARGETARCH \
        -c Release \
        --no-restore \
        -p:PublishSingleFile=false \
        -p:PublishTrimmed=false \
        -o /app/publish-api && \
    dotnet publish "PortfolioViewer/PortfolioViewer.WASM/PortfolioViewer.WASM.csproj" \
        -a $TARGETARCH \
        -c Release \
        --no-restore \
        -p:RunAOTCompilation=true \
        -p:PublishTrimmed=true \
        -o /app/publish-wasm

# Final runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final

# Create non-root user for security
RUN groupadd -r appuser && useradd -r -g appuser appuser

WORKDIR /app

# Install supervisor with minimal dependencies
RUN apt-get update && \
    apt-get install -y --no-install-recommends supervisor && \
    apt-get clean && \
    rm -rf /var/lib/apt/lists/* /tmp/* /var/tmp/*

# Copy published applications
COPY --from=build --chown=appuser:appuser /app/publish-api ./
COPY --from=build --chown=appuser:appuser /app/publish-wasm/wwwroot ./wwwroot
COPY --from=build --chown=appuser:appuser /app/publish-sidekick ./sidekick/

# Copy configuration files
COPY --chown=appuser:appuser supervisord.conf /etc/supervisor/conf.d/supervisord.conf

# Create directories and set permissions
RUN mkdir -p /https /var/log/supervisor && \
    chown -R appuser:appuser /app /https /var/log/supervisor

# Copy SSL certificate if it exists (make this optional)
COPY --chown=appuser:appuser certs/aspnetapp.pfx /https/aspnetapp.pfx 2>/dev/null || echo "No SSL certificate found, skipping..."

# Set environment variables
ENV ASPNETCORE_URLS="http://+:80;https://+:443" \
    ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_Kestrel__Certificates__Default__Path=/https/aspnetapp.pfx \
    ASPNETCORE_Kestrel__Certificates__Default__Password=YourPasswordHere \
    DOTNET_RUNNING_IN_CONTAINER=true \
    DOTNET_EnableDiagnostics=0

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=60s --retries=3 \
    CMD curl -f http://localhost:80/health || exit 1

# Switch to non-root user
USER appuser

EXPOSE 80
EXPOSE 443

# Use exec form for better signal handling
CMD ["supervisord", "-c", "/etc/supervisor/conf.d/supervisord.conf"]
