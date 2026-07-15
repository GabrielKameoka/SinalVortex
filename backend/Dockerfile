# Estágio de Build (.NET 10 SDK)
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build-env
WORKDIR /app

# Copia tudo o que o Docker acha que deve copiar
COPY . ./

# LISTA OS ARQUIVOS PARA O LOG DE BUILD DA RAILWAY (Isso vai salvar a nossa vida!)
RUN pwd && ls -la && ls -la SinalVortex.API || echo "Pasta API nao encontrada"

# Mantemos o comando padrão só para não quebrar a sintaxe do Dockerfile
RUN dotnet --version