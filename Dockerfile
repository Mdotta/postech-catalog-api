# Build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["catalog-api.csproj", "."]
RUN dotnet restore "catalog-api.csproj"

COPY . .
RUN dotnet build "catalog-api.csproj" -c Release -o /app/build

# Publish
FROM build AS publish
RUN dotnet publish "catalog-api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "catalog-api.dll"]
