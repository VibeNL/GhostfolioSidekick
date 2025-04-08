# Base runtime image for the API
FROM mcr.microsoft.com/dotnet/runtime:9.0 AS base
WORKDIR /app

# Build stage for the API
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Install Python, wasm-tools workload, and supervisord
RUN apt-get update && apt-get install -y python3 python3-pip supervisor && \
    dotnet workload install wasm-tools

# Copy and restore GhostfolioAPI
COPY ["GhostfolioAPI/GhostfolioAPI.csproj", "GhostfolioAPI/"]
RUN dotnet restore "GhostfolioAPI/GhostfolioAPI.csproj"

# Copy and restore PortfolioViewer.WASM
COPY ["PortfolioViewer/PortfolioViewer.WASM/PortfolioViewer.WASM.csproj", "PortfolioViewer.WASM/"]
RUN dotnet restore "PortfolioViewer.WASM/PortfolioViewer.WASM.csproj"

# Copy and restore GhostfolioSidekick
COPY ["GhostfolioSidekick/GhostfolioSidekick.csproj", "GhostfolioSidekick/"]
RUN dotnet restore "GhostfolioSidekick/GhostfolioSidekick.csproj"

# Copy the entire source code
COPY . .

# Build GhostfolioAPI
WORKDIR "/src/GhostfolioAPI"
RUN dotnet build "GhostfolioAPI.csproj" -c Release -o /app/build

# Build PortfolioViewer.WASM
WORKDIR "/src/PortfolioViewer/PortfolioViewer.WASM"
RUN dotnet build "PortfolioViewer.WASM.csproj" -c Release -o /app/build

# Publish GhostfolioAPI
FROM build AS publish-api
WORKDIR "/src/GhostfolioAPI"
RUN dotnet publish "GhostfolioAPI.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Publish PortfolioViewer.WASM (static files)
FROM build AS publish-wasm
WORKDIR "/src/PortfolioViewer/PortfolioViewer.WASM"
RUN dotnet publish "PortfolioViewer.WASM.csproj" -c Release -o /app/publish-wasm

# Publish GhostfolioSidekick
FROM build AS publish-sidekick
WORKDIR "/src/GhostfolioSidekick"
RUN dotnet publish "GhostfolioSidekick.csproj" -c Release -o /app/publish-sidekick

# Final stage: Combine API, WASM, and Sidekick
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Install supervisord
RUN apt-get update && apt-get install -y supervisor

# Copy API publish output
COPY --from=publish-api /app/publish .

# Copy WASM static files
COPY --from=publish-wasm /app/publish-wasm/wwwroot ./wwwroot

# Copy Sidekick publish output
COPY --from=publish-sidekick /app/publish-sidekick .

# Copy supervisord configuration
COPY supervisord.conf /etc/supervisor/conf.d/supervisord.conf

# Expose port
EXPOSE 80

# Start supervisord
CMD ["supervisord", "-c", "/etc/supervisor/conf.d/supervisord.conf"]
