# Estágio de Build (.NET 10 SDK)
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build-env
WORKDIR /app

# Copia tudo de uma vez para dentro do container
# Com o .dockerignore configurado acima, apenas o seu código limpo será copiado
COPY . ./

# Restaura as dependências usando a solução
RUN dotnet restore SinalVortex.slnx

# Compila e publica a API
RUN dotnet publish SinalVortex.API/SinalVortex.API.csproj -c Release -o out

# Estágio de Execução (.NET 10 Runtime)
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build-env /app/out .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "SinalVortex.API.dll"]