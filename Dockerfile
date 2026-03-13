# Build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY src/Postech.Catalog.Api/Postech.Catalog.Api.csproj src/Postech.Catalog.Api/
RUN dotnet restore src/Postech.Catalog.Api/Postech.Catalog.Api.csproj

COPY src/ src/
RUN dotnet publish src/Postech.Catalog.Api/Postech.Catalog.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

# Runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "catalog-api.dll"]
