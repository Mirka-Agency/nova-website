# syntax=docker/dockerfile:1

# -----------------------------------------------------------------------------
# Stage 1: minify first-party CSS/JS in wwwroot (paths/names unchanged).
# Node stays out of the final runtime image.
# -----------------------------------------------------------------------------
FROM node:22-bookworm-slim AS assets
WORKDIR /src

COPY tools/asset-minify/package.json tools/asset-minify/package-lock.json tools/asset-minify/
COPY tools/asset-minify/minify.js tools/asset-minify/
COPY src/CMS.Web/wwwroot ./src/CMS.Web/wwwroot

WORKDIR /src/tools/asset-minify
RUN npm ci \
  && npm run minify

# -----------------------------------------------------------------------------
# Stage 2: restore + publish .NET app with minified wwwroot overlaid.
# -----------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
COPY --from=assets /src/src/CMS.Web/wwwroot/ ./src/CMS.Web/wwwroot/
RUN dotnet restore src/CMS.Web/CMS.Web.csproj
RUN dotnet publish src/CMS.Web/CMS.Web.csproj -c Release -o /app/publish /p:UseAppHost=false

# -----------------------------------------------------------------------------
# Stage 3: production runtime (no Node.js).
# -----------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl tzdata \
    && rm -rf /var/lib/apt/lists/*
ENV TZ=Asia/Tehran
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
# CapRover / container deploys have no migrate job; docker-compose sets the same override.
ENV Database__MigrateOnStartup=true
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "CMS.Web.dll"]
