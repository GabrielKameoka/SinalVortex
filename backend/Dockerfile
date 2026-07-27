# Estágio de Build (.NET 10 SDK)
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build-env
WORKDIR /app

# Recebe qual projeto compilar (Padrão: SinalVortex.API)
ARG PROJECT_NAME=SinalVortex.API

# Copia todo o código do backend
COPY backend/ ./

# Restaura e publica o projeto informado no ARG
RUN dotnet restore ${PROJECT_NAME}/${PROJECT_NAME}.csproj
RUN dotnet publish ${PROJECT_NAME}/${PROJECT_NAME}.csproj -c Release -o out --no-restore

# Estágio de Execução (.NET 10 Runtime)
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build-env /app/out .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

# Recebe o nome da DLL no estágio final
ARG PROJECT_NAME=SinalVortex.API
ENV DLL_NAME=${PROJECT_NAME}.dll

# Executa dinamicamente a DLL gerada (API.dll ou Worker.dll)
ENTRYPOINT ["sh", "-c", "exec dotnet $DLL_NAME"]