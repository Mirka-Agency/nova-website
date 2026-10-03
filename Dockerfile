# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore src/CMS.Web/CMS.Web.csproj
RUN dotnet publish src/CMS.Web/CMS.Web.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
# CapRover / container deploys have no migrate job; docker-compose sets the same override.
ENV Database__MigrateOnStartup=true
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "CMS.Web.dll"]
