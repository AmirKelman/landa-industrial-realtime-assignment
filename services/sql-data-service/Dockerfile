FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY contracts ./contracts
COPY services/sql-data-service ./services/sql-data-service

WORKDIR /src/services/sql-data-service
RUN dotnet restore
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "sql-data-service.dll"]

