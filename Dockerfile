# Build stage for the unified container host and WASM client
FROM --platform="$BUILDPLATFORM" mcr.microsoft.com/dotnet/sdk:11.0 AS build

ARG TARGETARCH
ARG VERSION
ARG SourceRevisionId

WORKDIR /src

# Install Node.js (TypeScript compilation) and the wasm-tools workload in a single layer
RUN apt-get update && \
    curl -fsSL https://deb.nodesource.com/setup_24.x | bash - && \
    apt-get install -y nodejs && \
    dotnet workload install wasm-tools && \
    apt-get clean && rm -rf /var/lib/apt/lists/*

# Copy and restore projects to warm the NuGet package cache (transitive project references are restored again after the full source copy below). ContainerHost has no packages of its own, so it needs no pre-restore.
COPY ["PortfolioViewer/PortfolioViewer.ApiService/PortfolioViewer.ApiService.csproj", "PortfolioViewer.ApiService/"]
COPY ["PortfolioViewer/PortfolioViewer.WASM/PortfolioViewer.WASM.csproj", "PortfolioViewer.WASM/"]
COPY ["GhostfolioSidekick/GhostfolioSidekick.csproj", "GhostfolioSidekick/"]
# Central package management: versions live here, so it must exist before restore
COPY ["Directory.Packages.props", "./"]

RUN dotnet restore -a "$TARGETARCH" "PortfolioViewer.ApiService/PortfolioViewer.ApiService.csproj" && \
    dotnet restore -a "$TARGETARCH" "PortfolioViewer.WASM/PortfolioViewer.WASM.csproj" && \
    dotnet restore -a "$TARGETARCH" "GhostfolioSidekick/GhostfolioSidekick.csproj"

# Copy the entire source code
COPY . .

# Publish the unified host (includes the API service and sidekick assemblies)
FROM build AS publish-containerhost
WORKDIR "/src/ContainerHost"
RUN dotnet publish -a "$TARGETARCH" \
    "ContainerHost.csproj" \
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

# Final runtime image — Ubuntu chiseled (distroless form factor): no shell, no package manager.
# The "-extra" variant adds ICU + tzdata, required for culture-specific parsing in the sidekick.
FROM mcr.microsoft.com/dotnet/aspnet:11.0-resolute-chiseled-extra AS final

WORKDIR /app

# Copy published outputs (the unified host runs the API service and sidekick importer in one process)
COPY --from=publish-containerhost /app/publish ./
COPY --from=publish-wasm /app/publish-wasm/wwwroot ./wwwroot

# Copy SSL certificate and key (ensure these files are available in your build context)
COPY certs/aspnetapp.pfx /https/aspnetapp.pfx

# Set environment and expose ports
ENV ASPNETCORE_URLS="http://+:80;https://+:443"
ENV ASPNETCORE_Kestrel__Certificates__Default__Path=/https/aspnetapp.pfx
ENV ASPNETCORE_Kestrel__Certificates__Default__Password=YourPasswordHere

# Runs as the base image's unprivileged user (uid 1654, "app"). Docker grants
# CAP_NET_BIND_SERVICE by default, so Kestrel can still bind ports 80/443.

EXPOSE 80
EXPOSE 443

# Single process: the unified host runs the API service and sidekick importer in-process.
# The chiseled base image sets ENTRYPOINT ["/usr/bin/dotnet"], so CMD is just the app to run.
CMD ["/app/GhostfolioSidekick.ContainerHost.dll"]