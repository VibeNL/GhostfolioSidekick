# Base runtime image for the API
FROM --platform="$BUILDPLATFORM" mcr.microsoft.com/dotnet/runtime:9.0 AS base

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

# Build stage for the API, WASM and Sidekick
FROM --platform="$BUILDPLATFORM" mcr.microsoft.com/dotnet/sdk:9.0 AS build

ARG TARGETARCH
WORKDIR /src

# Install required native tools in one layer; keep packages minimal
RUN apt-get update && \
    apt-get install -y --no-install-recommends \
      ca-certificates \
      curl \
      gnupg \
      python3 \
      python3-pip \
      supervisor && \
    curl -fsSL https://deb.nodesource.com/setup_18.x | bash - && \
    apt-get install -y --no-install-recommends nodejs && \
    apt-get clean && rm -rf /var/lib/apt/lists/*

# If your Blazor WASM build requires the wasm-tools workload, install it here.
# This is cached between builds but will take time on the first run.
RUN dotenv workload install wasm-tools

# Copy project files first to leverage Docker cache for restores
COPY ["PortfolioViewer/PortfolioViewer.ApiService/PortfolioViewer.ApiService.csproj", "PortfolioViewer/PortfolioViewer.ApiService/"]
COPY ["PortfolioViewer/PortfolioViewer.WASM/PortfolioViewer.WASM.csproj", "PortfolioViewer/PortfolioViewer.WASM/"]
COPY ["GhostfolioSidekick/GhostfolioSidekick.csproj", "GhostfolioSidekick/"]

# Restore packages. Consider enabling BuildKit and using cache mounts for NuGet to speed this step.
# Example with BuildKit (uncomment when using BuildKit):
# RUN --mount=type=cache,target=/root/.nuget/packages dotnet restore "PortfolioViewer/PortfolioViewer.ApiService/PortfolioViewer.ApiService.csproj"
RUN dotnet restore "PortfolioViewer/PortfolioViewer.ApiService/PortfolioViewer.ApiService.csproj" && \
    dotnet restore "PortfolioViewer/PortfolioViewer.WASM/PortfolioViewer.WASM.csproj" && \
    dotnet restore "GhostfolioSidekick/GhostfolioSidekick.csproj"

# Copy the rest of the source after restore so changes to source files don't bust the NuGet cache
COPY . .

# Publish each project (use --no-restore because we already did restore)
RUN dotnet publish "PortfolioViewer/PortfolioViewer.ApiService/PortfolioViewer.ApiService.csproj" -c Release -o /app/publish --no-restore && \
    dotnet publish "PortfolioViewer/PortfolioViewer.WASM/PortfolioViewer.WASM.csproj" -c Release -o /app/publish-wasm --no-restore && \
    dotnet publish "GhostfolioSidekick/GhostfolioSidekick.csproj" -c Release -o /app/publish-sidekick --no-restore

# Final runtime image
FROM --platform="$BUILDPLATFORM" mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Install supervisord in final image
RUN apt-get update && \
    apt-get install -y --no-install-recommends supervisor && \
    apt-get clean && rm -rf /var/lib/apt/lists/*

# Copy published outputs from build stage
COPY --from=build /app/publish ./
COPY --from=build /app/publish-wasm/wwwroot ./wwwroot
COPY --from=build /app/publish-sidekick ./

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

# Notes:
# - Add a .dockerignore to reduce build context size (bin/, obj/, .git, node_modules, etc.).
# - To speed up restores with Docker BuildKit, use cache mounts for NuGet and npm.
# - The first run will be slower because of workload installation; subsequent builds will be faster due to caching.
