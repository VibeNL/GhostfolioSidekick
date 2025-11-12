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

# Build stage for the API and Sidekick
FROM --platform="$BUILDPLATFORM" mcr.microsoft.com/dotnet/sdk:10.0 AS build

ARG TARGETARCH

WORKDIR /src

# Install Python, wasm-tools workload, and supervisord in a single layer
RUN apt-get update && \
    apt-get install -y python3 python3-pip supervisor && \
    curl -fsSL https://deb.nodesource.com/setup_18.x | bash - && \
    apt-get install -y nodejs && \
    dotnet workload install wasm-tools && \
    apt-get clean && rm -rf /var/lib/apt/lists/*

# Copy and restore projects
COPY ["PortfolioViewer/PortfolioViewer.ApiService/PortfolioViewer.ApiService.csproj", "PortfolioViewer.ApiService/"]
COPY ["PortfolioViewer/PortfolioViewer.WASM/PortfolioViewer.WASM.csproj", "PortfolioViewer.WASM/"]
COPY ["GhostfolioSidekick/GhostfolioSidekick.csproj", "GhostfolioSidekick/"]

RUN dotnet restore -a "$TARGETARCH" "PortfolioViewer.ApiService/PortfolioViewer.ApiService.csproj" && \
    dotnet restore -a "$TARGETARCH" "PortfolioViewer.WASM/PortfolioViewer.WASM.csproj" && \
    dotnet restore -a "$TARGETARCH" "GhostfolioSidekick/GhostfolioSidekick.csproj"

# Copy the entire source code
COPY . .

# Build all projects
RUN dotnet build "PortfolioViewer/PortfolioViewer.ApiService/PortfolioViewer.ApiService.csproj" -c Release -o /app/build && \
    dotnet build "PortfolioViewer/PortfolioViewer.WASM/PortfolioViewer.WASM.csproj" -c Release -o /app/build && \
    dotnet build "GhostfolioSidekick/GhostfolioSidekick.csproj" -c Release -o /app/build

# Publish each project
FROM build AS publish-api
WORKDIR "/src/PortfolioViewer/PortfolioViewer.ApiService"
RUN dotnet publish -a "$TARGETARCH" "PortfolioViewer.ApiService.csproj" -c Release -o /app/publish

FROM build AS publish-wasm
WORKDIR "/src/PortfolioViewer/PortfolioViewer.WASM"
RUN dotnet publish -a "$TARGETARCH" "PortfolioViewer.WASM.csproj" -c Release -o /app/publish-wasm

FROM build AS publish-sidekick
WORKDIR "/src/GhostfolioSidekick"
RUN dotnet publish -a "$TARGETARCH" "GhostfolioSidekick.csproj" -c Release -o /app/publish-sidekick

# Final runtime image
FROM --platform="$BUILDPLATFORM" mcr.microsoft.com/dotnet/aspnet:10.0 AS final

WORKDIR /app

# Install supervisord
RUN apt-get update && \
    apt-get install -y supervisor && \
    apt-get clean && rm -rf /var/lib/apt/lists/*

# Copy published outputs
COPY --from=publish-api /app/publish ./
COPY --from=publish-wasm /app/publish-wasm/wwwroot ./wwwroot
COPY --from=publish-sidekick /app/publish-sidekick ./

# Copy supervisord config
COPY supervisord.conf /etc/supervisor/conf.d/supervisord.conf

# Copy SSL certificate and key (ensure these files are available in your build context)
COPY certs/aspnetapp.pfx /https/aspnetapp.pfx

# Set environment and expose ports
ENV ASPNETCORE_URLS="http://+:80;https://+:443"
ENV ASPNETCORE_Kestrel__Certificates__Default__Path=/https/aspnetapp.pfx
ENV ASPNETCORE_Kestrel__Certificates__Default__Password=YourPasswordHere

EXPOSE 80
EXPOSE 443

# Start app via supervisord
CMD ["supervisord", "-c", "/etc/supervisor/conf.d/supervisord.conf"]
