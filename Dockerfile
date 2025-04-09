# Base runtime image for the API
FROM mcr.microsoft.com/dotnet/runtime:9.0 AS base
WORKDIR /app

# Build stage for the API
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Install Python, wasm-tools workload, and supervisord in a single layer
RUN apt-get update && apt-get install -y python3 python3-pip supervisor && \
    dotnet workload install wasm-tools && \
    apt-get clean && rm -rf /var/lib/apt/lists/*

# Copy and restore all projects in a single step to maximize caching
COPY ["PortfolioViewer/PortfolioViewer.ApiService/PortfolioViewer.ApiService.csproj", "PortfolioViewer.ApiService/"]
COPY ["PortfolioViewer/PortfolioViewer.WASM/PortfolioViewer.WASM.csproj", "PortfolioViewer.WASM/"]
COPY ["GhostfolioSidekick/GhostfolioSidekick.csproj", "GhostfolioSidekick/"]
RUN dotnet restore "PortfolioViewer.ApiService/PortfolioViewer.ApiService.csproj" && \
    dotnet restore "PortfolioViewer.WASM/PortfolioViewer.WASM.csproj" && \
    dotnet restore "GhostfolioSidekick/GhostfolioSidekick.csproj"

# Copy the entire source code
COPY . .

# Build all projects in a single step to reduce layers
RUN dotnet build "PortfolioViewer/PortfolioViewer.ApiService/PortfolioViewer.ApiService.csproj" -c Release -o /app/build && \
    dotnet build "PortfolioViewer/PortfolioViewer.WASM/PortfolioViewer.WASM.csproj" -c Release -o /app/build && \
    dotnet build "GhostfolioSidekick/GhostfolioSidekick.csproj" -c Release -o /app/build

# Publish all projects in parallel stages
FROM build AS publish-api
WORKDIR "/src/PortfolioViewer/PortfolioViewer.ApiService"
RUN dotnet publish "PortfolioViewer.ApiService.csproj" -c Release -o /app/publish

FROM build AS publish-wasm
WORKDIR "/src/PortfolioViewer/PortfolioViewer.WASM"
RUN dotnet publish "PortfolioViewer.WASM.csproj" -c Release -o /app/publish-wasm

FROM build AS publish-sidekick
WORKDIR "/src/GhostfolioSidekick"
RUN dotnet publish "GhostfolioSidekick.csproj" -c Release -o /app/publish-sidekick

# Final stage: Combine API, WASM, and Sidekick
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Install supervisord in a single layer
RUN apt-get update && apt-get install -y supervisor && \
    apt-get clean && rm -rf /var/lib/apt/lists/*

# Copy API publish output
COPY --from=publish-api /app/publish ./

# Copy WASM static files
COPY --from=publish-wasm /app/publish-wasm/wwwroot ./wwwroot

# Copy Sidekick publish output
COPY --from=publish-sidekick /app/publish-sidekick ./

# Copy supervisord configuration
COPY supervisord.conf /etc/supervisor/conf.d/supervisord.conf

# Expose port
ENV ASPNETCORE_URLS=http://+:80
EXPOSE 80

# Start supervisord
CMD ["supervisord", "-c", "/etc/supervisor/conf.d/supervisord.conf"]
