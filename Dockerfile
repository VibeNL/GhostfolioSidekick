# Node.js source (avoids apt-get for nodejs)
# Must match BUILDPLATFORM so the node binary can run on the build machine during cross-compilation
FROM --platform="$BUILDPLATFORM" node:18-slim AS node-source

# Python source - provides Python (required by wasm-tools/Emscripten) and supervisor
# Must match BUILDPLATFORM so binaries run on the build machine during cross-compilation
FROM --platform="$BUILDPLATFORM" python:3.12-slim AS python-source
RUN pip install --no-cache-dir supervisor

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
ARG VERSION
ARG SourceRevisionId

WORKDIR /src

# Copy Node.js binaries from official image (no apt-get needed)
COPY --from=node-source /usr/local/bin/node /usr/local/bin/node
COPY --from=node-source /usr/local/bin/npm /usr/local/bin/npm
COPY --from=node-source /usr/local/bin/npx /usr/local/bin/npx
COPY --from=node-source /usr/local/lib/node_modules /usr/local/lib/node_modules/

# Copy Python from official image (required by wasm-tools Emscripten toolchain)
COPY --from=python-source /usr/local/bin/python3.12 /usr/local/bin/python3.12
COPY --from=python-source /usr/local/lib/python3.12 /usr/local/lib/python3.12/
COPY --from=python-source /usr/local/lib/libpython3.12.so.1.0 /usr/local/lib/libpython3.12.so.1.0
RUN ln -sf /usr/local/bin/python3.12 /usr/local/bin/python3 && \
    ln -sf /usr/local/bin/python3 /usr/local/bin/python && \
    ldconfig

# Install wasm-tools workload
RUN dotnet workload install wasm-tools

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


# Final runtime image
FROM --platform="$BUILDPLATFORM" mcr.microsoft.com/dotnet/aspnet:10.0 AS final

WORKDIR /app

# Copy Python runtime and supervisor from pre-built stage (no apt-get needed)
COPY --from=python-source /usr/local/bin/python3.12 /usr/local/bin/python3.12
COPY --from=python-source /usr/local/lib/python3.12 /usr/local/lib/python3.12/
COPY --from=python-source /usr/local/lib/libpython3.12.so.1.0 /usr/local/lib/libpython3.12.so.1.0
COPY --from=python-source /usr/local/bin/supervisord /usr/local/bin/supervisord
COPY --from=python-source /usr/local/bin/supervisorctl /usr/local/bin/supervisorctl
RUN ln -sf /usr/local/bin/python3.12 /usr/local/bin/python3 && ldconfig

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