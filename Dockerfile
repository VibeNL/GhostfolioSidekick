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

# Build stage: only restore/build/publish the needed projects
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

WORKDIR /src

# Copy only the csproj files and restore as distinct layers
COPY PortfolioViewer/PortfolioViewer.ApiService/PortfolioViewer.ApiService.csproj PortfolioViewer/PortfolioViewer.ApiService/
COPY PortfolioViewer/PortfolioViewer.WASM/PortfolioViewer.WASM.csproj PortfolioViewer/PortfolioViewer.WASM/
COPY GhostfolioSidekick/GhostfolioSidekick.csproj GhostfolioSidekick/

RUN apt-get update \
 && apt-get install -y python3 python3-pip supervisor \
 && curl -fsSL https://deb.nodesource.com/setup_18.x | bash - \
 && apt-get install -y nodejs \
 && dotnet workload install wasm-tools \
 && apt-get clean \
 && rm -rf /var/lib/apt/lists/*

RUN dotnet restore PortfolioViewer/PortfolioViewer.ApiService/PortfolioViewer.ApiService.csproj \
 && dotnet restore PortfolioViewer/PortfolioViewer.WASM/PortfolioViewer.WASM.csproj \
 && dotnet restore GhostfolioSidekick/GhostfolioSidekick.csproj

# Copy everything else and build/publish
COPY . .

RUN dotnet publish PortfolioViewer/PortfolioViewer.ApiService/PortfolioViewer.ApiService.csproj -c Release -o /app/publish-api
RUN dotnet publish PortfolioViewer/PortfolioViewer.WASM/PortfolioViewer.WASM.csproj -c Release -o /app/publish-wasm
RUN dotnet publish GhostfolioSidekick/GhostfolioSidekick.csproj -c Release -o /app/publish-sidekick

# Final runtime image: only copy published output
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final

WORKDIR /app

RUN apt-get update \
 && apt-get install -y supervisor \
 && apt-get clean \
 && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish-api ./
COPY --from=build /app/publish-wasm/wwwroot ./wwwroot
COPY --from=build /app/publish-sidekick ./

COPY supervisord.conf /etc/supervisor/conf.d/supervisord.conf
COPY certs/aspnetapp.pfx /https/aspnetapp.pfx

ENV ASPNETCORE_URLS="http://+:80;https://+:443"
ENV ASPNETCORE_Kestrel__Certificates__Default__Path=/https/aspnetapp.pfx
ENV ASPNETCORE_Kestrel__Certificates__Default__Password=YourPasswordHere

EXPOSE 80
EXPOSE 443

CMD ["supervisord", "-c", "/etc/supervisor/conf.d/supervisord.conf"]
