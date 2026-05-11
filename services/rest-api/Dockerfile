FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY contracts ./contracts
COPY services/rest-api ./services/rest-api

WORKDIR /src/services/rest-api
RUN dotnet restore
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "rest-api.dll"]

