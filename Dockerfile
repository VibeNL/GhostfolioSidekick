# Build stage for the API and Sidekick
FROM --platform="$BUILDPLATFORM" mcr.microsoft.com/dotnet/sdk:10.0 AS build

ARG TARGETARCH

WORKDIR /src

# Install Node.js and wasm-tools workload in a single layer (Python removed - not used)
RUN apt-get update && \
    curl -fsSL https://deb.nodesource.com/setup_22.x | bash - && \
    apt-get install -y nodejs && \
    dotnet workload install wasm-tools && \
    apt-get clean && rm -rf /var/lib/apt/lists/*

# Copy solution file, build props, and all project files for better caching
COPY ["GhostfolioSidekick.slnx", "./"]
COPY ["Directory.Build.props", "./"]
COPY ["Database/Database.csproj", "Database/"]
COPY ["Cryptocurrency/Cryptocurrency.csproj", "Cryptocurrency/"]
COPY ["ExternalDataProvider.UnitTests/ExternalDataProvider.UnitTests.csproj", "ExternalDataProvider.UnitTests/"]
COPY ["Configuration.UnitTests/Configuration.UnitTests.csproj", "Configuration.UnitTests/"]
COPY ["Model/Model.csproj", "Model/"]
COPY ["Cryptocurrency.UnitTests/Cryptocurrency.UnitTests.csproj", "Cryptocurrency.UnitTests/"]
COPY ["Parsers/Parsers.csproj", "Parsers/"]
COPY ["GhostfolioAPI/GhostfolioAPI.csproj", "GhostfolioAPI/"]
COPY ["GhostfolioAPI.UnitTests/GhostfolioAPI.UnitTests.csproj", "GhostfolioAPI.UnitTests/"]
COPY ["GhostfolioSidekick/GhostfolioSidekick.csproj", "GhostfolioSidekick/"]
COPY ["GhostfolioSidekick.UnitTests/GhostfolioSidekick.UnitTests.csproj", "GhostfolioSidekick.UnitTests/"]
COPY ["Database.UnitTests/Database.UnitTests.csproj", "Database.UnitTests/"]
COPY ["PerformanceCalculations/PerformanceCalculations.csproj", "PerformanceCalculations/"]
COPY ["Model.UnitTests/Model.UnitTests.csproj", "Model.UnitTests/"]
COPY ["Configuration/Configuration.csproj", "Configuration/"]
COPY ["IntegrationTests/IntegrationTests.csproj", "IntegrationTests/"]
COPY ["AI/AI.Agents/AI.Agents.csproj", "AI/AI.Agents/"]
COPY ["AI/AI.Common/AI.Common.csproj", "AI/AI.Common/"]
COPY ["Utilities/Utilities.csproj", "Utilities/"]
COPY ["AI/AI.Functions/AI.Functions.csproj", "AI/AI.Functions/"]
COPY ["Utilities.UnitTests/Utilities.UnitTests.csproj", "Utilities.UnitTests/"]
COPY ["PerformanceCalculations.UnitTests/PerformanceCalculations.UnitTests.csproj", "PerformanceCalculations.UnitTests/"]
COPY ["AI/AI.Server.UnitTests/AI.Server.UnitTests.csproj", "AI/AI.Server.UnitTests/"]
COPY ["AI/AI.Server/AI.Server.csproj", "AI/AI.Server/"]
COPY ["AI/AI.Functions.UnitTests/AI.Functions.UnitTests.csproj", "AI/AI.Functions.UnitTests/"]
COPY ["AI/AI.Agents.UnitTests/AI.Agents.UnitTests.csproj", "AI/AI.Agents.UnitTests/"]
COPY ["Tools/ScraperUtilities/ScraperUtilities.csproj", "Tools/ScraperUtilities/"]
COPY ["Tools/AnonymisePDF/AnonymisePDF.csproj", "Tools/AnonymisePDF/"]
COPY ["Tools/AnonymisePDF.UnitTests/AnonymisePDF.UnitTests.csproj", "Tools/AnonymisePDF.UnitTests/"]
COPY ["PortfolioViewer/PortfolioViewer.AppHost/PortfolioViewer.AppHost.csproj", "PortfolioViewer/PortfolioViewer.AppHost/"]
COPY ["Parsers.UnitTests/Parsers.UnitTests.csproj", "Parsers.UnitTests/"]
COPY ["PortfolioViewer/PortfolioViewer.Common/PortfolioViewer.Common.csproj", "PortfolioViewer/PortfolioViewer.Common/"]
COPY ["PortfolioViewer/PortfolioViewer.Tests/PortfolioViewer.Tests.csproj", "PortfolioViewer/PortfolioViewer.Tests/"]
COPY ["PortfolioViewer/PortfolioViewer.WASM.Data/PortfolioViewer.WASM.Data.csproj", "PortfolioViewer/PortfolioViewer.WASM.Data/"]
COPY ["PortfolioViewer/PortfolioViewer.ServiceDefaults/PortfolioViewer.ServiceDefaults.csproj", "PortfolioViewer/PortfolioViewer.ServiceDefaults/"]
COPY ["PortfolioViewer/PortfolioViewer.ApiService.UnitTests/PortfolioViewer.ApiService.UnitTests.csproj", "PortfolioViewer/PortfolioViewer.ApiService.UnitTests/"]
COPY ["ExternalDataProvider/ExternalDataProvider.csproj", "ExternalDataProvider/"]
COPY ["PortfolioViewer/PortfolioViewer.WASM.UnitTests/PortfolioViewer.WASM.UnitTests.csproj", "PortfolioViewer/PortfolioViewer.WASM.UnitTests/"]
COPY ["PortfolioViewer/PortfolioViewer.WASM.AI.UnitTests/PortfolioViewer.WASM.AI.UnitTests.csproj", "PortfolioViewer/PortfolioViewer.WASM.AI.UnitTests/"]
COPY ["PortfolioViewer/PortfolioViewer.WASM.Data.UnitTests/PortfolioViewer.WASM.Data.UnitTests.csproj", "PortfolioViewer/PortfolioViewer.WASM.Data.UnitTests/"]
COPY ["PortfolioViewer/PortfolioViewer.WASM.AI/PortfolioViewer.WASM.AI.csproj", "PortfolioViewer/PortfolioViewer.WASM.AI/"]
COPY ["PortfolioViewer/PortfolioViewer.WASM.UITests/PortfolioViewer.WASM.UITests.csproj", "PortfolioViewer/PortfolioViewer.WASM.UITests/"]
COPY ["PortfolioViewer/PortfolioViewer.WASM/PortfolioViewer.WASM.csproj", "PortfolioViewer/PortfolioViewer.WASM/"]
COPY ["PortfolioViewer/PortfolioViewer.ApiService/PortfolioViewer.ApiService.csproj", "PortfolioViewer/PortfolioViewer.ApiService/"]

# Restore all dependencies using the solution file
RUN dotnet restore -a "$TARGETARCH" "GhostfolioSidekick.slnx"

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