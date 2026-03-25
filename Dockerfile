
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS base
WORKDIR /repo

FROM base AS build
COPY src ./src
COPY Postech.Shared.dll ./Postech.Shared.dll
WORKDIR /repo/src/Postech.Catalog.Api
RUN dotnet restore Postech.Catalog.Api.csproj
RUN dotnet build Postech.Catalog.Api.csproj -c Release -o /app/build

FROM build AS test
WORKDIR /repo/src/Postech.Catalog.Api.Tests
RUN dotnet test Postech.Catalog.Api.Tests.csproj -c Release --verbosity normal

FROM build AS publish
WORKDIR /repo/src/Postech.Catalog.Api
RUN dotnet publish Postech.Catalog.Api.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
EXPOSE 80
ENTRYPOINT ["dotnet", "Postech.Catalog.Api.dll"]
