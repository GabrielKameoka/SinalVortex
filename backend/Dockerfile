# Estágio de Build (.NET 10 SDK)
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build-env
WORKDIR /app

# Copia a solução e restaura as dependências respeitando o monorepo
COPY SinalVortex.sln ./
COPY src/1.Domain/SinalVortex.Domain.csproj src/1.Domain/
COPY src/2.Application/SinalVortex.Application.csproj src/2.Application/
COPY src/3.Infrastructure/SinalVortex.Infrastructure.csproj src/3.Infrastructure/
COPY src/4.API/SinalVortex.API.csproj src/4.API/
COPY src/5.Worker/SinalVortex.Worker.csproj src/5.Worker/

RUN dotnet restore SinalVortex.sln

# Copia todo o restante do código e compila a API
COPY . ./
RUN dotnet publish src/4.API/SinalVortex.API.csproj -c Release -o out

# Estágio de Execução (.NET 10 Runtime)
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build-env /app/out .

# Expõe a porta padrão do ASP.NET
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "SinalVortex.API.dll"]