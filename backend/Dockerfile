# Estágio de Build (.NET 10 SDK)
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build-env
WORKDIR /app

# Copia a solução que está no diretório atual
COPY SinalVortex.slnx ./

# Copia as pastas inteiras de cada projeto em vez de ficheiros específicos, 
# o que evita falhas de caminho estrito do Docker Daemon
COPY SinalVortex.Domain/ SinalVortex.Domain/
COPY SinalVortex.Application/ SinalVortex.Application/
COPY SinalVortex.Infrastructure/ SinalVortex.Infrastructure/
COPY SinalVortex.API/ SinalVortex.API/
COPY SinalVortex.Worker/ SinalVortex.Worker/

# Restaura as dependências
RUN dotnet restore SinalVortex.slnx

# Copia o resto do código
COPY . ./

# Publica a API
RUN dotnet publish SinalVortex.API/SinalVortex.API.csproj -c Release -o out

# Estágio de Execução (.NET 10 Runtime)
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build-env /app/out .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "SinalVortex.API.dll"]