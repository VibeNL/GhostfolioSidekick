# Base runtime image for the API
FROM mcr.microsoft.com/dotnet/runtime:9.0 AS base
WORKDIR /app

# Build stage for the API
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Install Python and wasm-tools workload
RUN apt-get update && apt-get install -y python3 python3-pip && \
    dotnet workload install wasm-tools

# Copy and restore GhostfolioAPI
COPY ["GhostfolioAPI/GhostfolioAPI.csproj", "GhostfolioAPI/"]
RUN dotnet restore "GhostfolioAPI/GhostfolioAPI.csproj"

# Copy and restore PortfolioViewer.WASM
COPY ["PortfolioViewer/PortfolioViewer.WASM/PortfolioViewer.WASM.csproj", "PortfolioViewer/PortfolioViewer.WASM/"]
RUN dotnet restore "PortfolioViewer/PortfolioViewer.WASM/PortfolioViewer.WASM.csproj"

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

# Final stage: Combine API and WASM
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Copy API publish output
COPY --from=publish-api /app/publish .

# Copy WASM static files
COPY --from=publish-wasm /app/publish-wasm/wwwroot ./wwwroot

# Expose port and set entry point
EXPOSE 80
ENTRYPOINT ["dotnet", "GhostfolioSidekick.GhostfolioAPI.dll"]
