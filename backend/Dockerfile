# Estágio de Build (.NET 10 SDK)
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build-env
WORKDIR /app

# Copia todo o código do backend
COPY backend/ ./

# Restaura as dependências da API
RUN dotnet restore SinalVortex.API/SinalVortex.API.csproj

# Compila e publica a API
RUN dotnet publish SinalVortex.API/SinalVortex.API.csproj -c Release -o out --no-restore

# Estágio de Execução (.NET 10 Runtime)
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build-env /app/out .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "SinalVortex.API.dll"]