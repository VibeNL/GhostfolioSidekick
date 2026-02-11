# Build stage for the API and Sidekick
FROM --platform="$BUILDPLATFORM" mcr.microsoft.com/dotnet/sdk:10.0 AS build

ARG TARGETARCH

WORKDIR /src

# Install Node.js and wasm-tools workload in a single layer (Python removed - not used)
RUN apt-get update && \
    curl -fsSL https://deb.nodesource.com/setup_18.x | bash - && \
    apt-get install -y nodejs && \
    dotnet workload install wasm-tools && \
    apt-get clean && rm -rf /var/lib/apt/lists/*

# Copy solution file and all project files for better caching
COPY ["GhostfolioSidekick.slnx", "./"]
COPY ["Directory.Build.props", "./"]
COPY ["PortfolioViewer/PortfolioViewer.ApiService/PortfolioViewer.ApiService.csproj", "PortfolioViewer/PortfolioViewer.ApiService/"]
COPY ["PortfolioViewer/PortfolioViewer.WASM/PortfolioViewer.WASM.csproj", "PortfolioViewer/PortfolioViewer.WASM/"]
COPY ["PortfolioViewer/PortfolioViewer.WASM.Data/PortfolioViewer.WASM.Data.csproj", "PortfolioViewer/PortfolioViewer.WASM.Data/"]
COPY ["PortfolioViewer/PortfolioViewer.Common/PortfolioViewer.Common.csproj", "PortfolioViewer/PortfolioViewer.Common/"]
COPY ["PortfolioViewer/PortfolioViewer.ServiceDefaults/PortfolioViewer.ServiceDefaults.csproj", "PortfolioViewer/PortfolioViewer.ServiceDefaults/"]
COPY ["GhostfolioSidekick/GhostfolioSidekick.csproj", "GhostfolioSidekick/"]
COPY ["Database/Database.csproj", "Database/"]
COPY ["GhostfolioAPI/GhostfolioAPI.csproj", "GhostfolioAPI/"]
COPY ["Model/Model.csproj", "Model/"]
COPY ["Configuration/Configuration.csproj", "Configuration/"]
COPY ["Utilities/Utilities.csproj", "Utilities/"]

# Restore all dependencies in one step
RUN dotnet restore -a "$TARGETARCH" "PortfolioViewer/PortfolioViewer.ApiService/PortfolioViewer.ApiService.csproj" && \
    dotnet restore -a "$TARGETARCH" "PortfolioViewer/PortfolioViewer.WASM/PortfolioViewer.WASM.csproj" && \
    dotnet restore -a "$TARGETARCH" "GhostfolioSidekick/GhostfolioSidekick.csproj"

# Copy only source files needed for build (use .dockerignore to exclude tests, docs, etc.)
COPY . .

# Publish each project directly (no separate build step - publish does build)
FROM build AS publish-api
WORKDIR "/src/PortfolioViewer/PortfolioViewer.ApiService"
RUN dotnet publish -a "$TARGETARCH" --no-restore -c Release -o /app/publish

FROM build AS publish-wasm
WORKDIR "/src/PortfolioViewer/PortfolioViewer.WASM"
RUN dotnet publish -a "$TARGETARCH" --no-restore -c Release -o /app/publish-wasm

FROM build AS publish-sidekick
WORKDIR "/src/GhostfolioSidekick"
RUN dotnet publish -a "$TARGETARCH" --no-restore -c Release -o /app/publish-sidekick

# Final runtime image - use TARGETPLATFORM for runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final

ARG TARGETARCH

WORKDIR /app

# Install supervisord only (removed duplicate Python/Node.js)
RUN apt-get update && \
    apt-get install -y supervisor && \
    apt-get clean && rm -rf /var/lib/apt/lists/*

# Copy published outputs
COPY --from=publish-api /app/publish ./
COPY --from=publish-wasm /app/publish-wasm/wwwroot ./wwwroot
COPY --from=publish-sidekick /app/publish-sidekick ./

# Copy supervisord config
COPY supervisord.conf /etc/supervisor/conf.d/supervisord.conf

# Copy SSL certificate (use build secret or volume mount instead of embedding in image)
COPY certs/aspnetapp.pfx /https/aspnetapp.pfx

# Set environment and expose ports
ENV ASPNETCORE_URLS="http://+:80;https://+:443" \
    ASPNETCORE_Kestrel__Certificates__Default__Path=/https/aspnetapp.pfx
# NOTE: Set password via environment variable at runtime, not hardcoded here
# Example: docker run -e ASPNETCORE_Kestrel__Certificates__Default__Password=YourPassword ...

EXPOSE 80
EXPOSE 443

# Start app via supervisord
CMD ["supervisord", "-c", "/etc/supervisor/conf.d/supervisord.conf"]