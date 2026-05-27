# Base runtime image for the API
FROM --platform="$BUILDPLATFORM" mcr.microsoft.com/dotnet/runtime:10.0 AS base

ARG TARGETPLATFORM
ARG TARGETOS
ARG TARGETARCH
ARG TARGETVARIANT
ARG BUILDPLATFORM
ARG BUILDOS
ARG BUILDARCH
ARG BUILDVARIANT

RUN echo "Building on $BUILDPLATFORM, targeting $TARGETPLATFORM"
RUN echo "Building on ${BUILDOS} and ${BUILDARCH} with optional variant ${BUILDVARIANT}"
RUN echo "Targeting ${TARGETOS} and ${TARGETARCH} with optional variant ${TARGETVARIANT}"

WORKDIR /app

# Build stage
FROM --platform="$BUILDPLATFORM" mcr.microsoft.com/dotnet/sdk:10.0 AS build

ARG TARGETARCH
ARG VERSION
ARG SourceRevisionId

WORKDIR /src

# Install Python, Node.js, and wasm-tools workload in a single layer
RUN apt-get update && \
    apt-get install -y python3 python3-pip && \
    curl -fsSL https://deb.nodesource.com/setup_18.x | bash - && \
    apt-get install -y nodejs && \
    dotnet workload install wasm-tools && \
    apt-get clean && rm -rf /var/lib/apt/lists/*

# Copy and restore projects
COPY ["PortfolioViewer/PortfolioViewer.ApiService/PortfolioViewer.ApiService.csproj", "PortfolioViewer.ApiService/"]
COPY ["PortfolioViewer/PortfolioViewer.WASM/PortfolioViewer.WASM.csproj", "PortfolioViewer.WASM/"]
COPY ["GhostfolioSidekick/GhostfolioSidekick.csproj", "GhostfolioSidekick/"]
COPY ["ProcessHost/ProcessHost.csproj", "ProcessHost/"]

RUN dotnet restore -a "$TARGETARCH" "PortfolioViewer.ApiService/PortfolioViewer.ApiService.csproj" && \
    dotnet restore -a "$TARGETARCH" "PortfolioViewer.WASM/PortfolioViewer.WASM.csproj" && \
    dotnet restore -a "$TARGETARCH" "GhostfolioSidekick/GhostfolioSidekick.csproj" && \
    dotnet restore -a "$TARGETARCH" "ProcessHost/ProcessHost.csproj"

# Copy the entire source code
COPY . .

# Build all projects
RUN dotnet build "PortfolioViewer/PortfolioViewer.ApiService/PortfolioViewer.ApiService.csproj" -c Release -o /app/build && \
    dotnet build "PortfolioViewer/PortfolioViewer.WASM/PortfolioViewer.WASM.csproj" -c Release -o /app/build && \
    dotnet build "GhostfolioSidekick/GhostfolioSidekick.csproj" -c Release -o /app/build && \
    dotnet build "ProcessHost/ProcessHost.csproj" -c Release -o /app/build

# Publish each project
FROM build AS publish-api
WORKDIR "/src/PortfolioViewer/PortfolioViewer.ApiService"
RUN dotnet publish -a "$TARGETARCH" \
    "PortfolioViewer.ApiService.csproj" \
    -c Release \
    -o /app/publish \
    /p:SourceRevisionId="$SourceRevisionId"

FROM build AS publish-wasm
WORKDIR "/src/PortfolioViewer/PortfolioViewer.WASM"
RUN dotnet publish -a "$TARGETARCH" \
    "PortfolioViewer.WASM.csproj" \
    -c Release \
    -o /app/publish-wasm \
    /p:SourceRevisionId="$SourceRevisionId"

FROM build AS publish-sidekick
WORKDIR "/src/GhostfolioSidekick"
RUN dotnet publish -a "$TARGETARCH" \
    "GhostfolioSidekick.csproj" \
    -c Release \
    -o /app/publish-sidekick \
    /p:SourceRevisionId="$SourceRevisionId"

FROM build AS publish-host
WORKDIR "/src/ProcessHost"
RUN dotnet publish -a "$TARGETARCH" \
    "ProcessHost.csproj" \
    -c Release \
    -o /app/publish-host \
    /p:SourceRevisionId="$SourceRevisionId"

# ──────────────────────────────────────────────────────────────────────────────
# Final image — Ubuntu Chiseled (distroless, non-root)
# ProcessHost is PID 1; it starts ApiService and GhostfolioSidekick as children.
# ──────────────────────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled AS final

WORKDIR /app

# ProcessHost entry point
COPY --from=publish-host /app/publish-host ./

# ApiService DLLs (ProcessHost launches these via dotnet <dll>)
COPY --from=publish-api /app/publish ./

# Sidekick DLLs
COPY --from=publish-sidekick /app/publish-sidekick ./

# Blazor WASM static files served by the ApiService
COPY --from=publish-wasm /app/publish-wasm/wwwroot ./wwwroot

# TLS certificate
COPY certs/aspnetapp.pfx /https/aspnetapp.pfx

ENV ASPNETCORE_URLS="http://+:80;https://+:443"
ENV ASPNETCORE_Kestrel__Certificates__Default__Path=/https/aspnetapp.pfx
ENV ASPNETCORE_Kestrel__Certificates__Default__Password=YourPasswordHere

EXPOSE 80
EXPOSE 443

# Chiseled images run as non-root user "app" by default
USER app

ENTRYPOINT ["dotnet", "GhostfolioSidekick.ProcessHost.dll"]