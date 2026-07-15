# Estágio de Build (.NET 10 SDK)
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build-env
WORKDIR /app

# Copia o ficheiro de solução novo (.slnx)
COPY SinalVortex.slnx ./

# Copia os ficheiros de projeto (.csproj) respeitando a tua estrutura real (sem "src" e sem números)
COPY SinalVortex.Domain/SinalVortex.Domain.csproj SinalVortex.Domain/
COPY SinalVortex.Application/SinalVortex.Application.csproj SinalVortex.Application/
COPY SinalVortex.Infrastructure/SinalVortex.Infrastructure.csproj SinalVortex.Infrastructure/
COPY SinalVortex.API/SinalVortex.API.csproj SinalVortex.API/
COPY SinalVortex.Worker/SinalVortex.Worker.csproj SinalVortex.Worker/

# Restaura as dependências usando o novo formato de solução do .NET
RUN dotnet restore SinalVortex.slnx

# Copia todo o código restante do backend
COPY . ./

# Publica a API diretamente a partir da pasta correta
RUN dotnet publish SinalVortex.API/SinalVortex.API.csproj -c Release -o out

# Estágio de Execução (.NET 10 Runtime)
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build-env /app/out .

# Expõe a porta que a Railway vai escutar
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "SinalVortex.API.dll"]