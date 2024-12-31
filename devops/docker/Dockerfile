FROM mcr.microsoft.com/dotnet/runtime:9.0 AS base

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY --link . .

RUN dotnet build && dotnet publish -o /app

FROM base AS final
WORKDIR /app

COPY --link --from=build /app .

ENTRYPOINT ["dotnet", "Curity.DbMaint.dll"]