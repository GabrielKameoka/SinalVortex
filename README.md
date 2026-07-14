# SinalVortex - Monorepo

Um monorepo completo com backend em .NET 10 e frontend em Angular 17.

## 📋 Estrutura do Projeto

```
SinalVortex/
├── backend/
│   ├── SinalVortex.Domain/          # Entidades de domínio
│   ├── SinalVortex.Application/     # Serviços e lógica de aplicação
│   ├── SinalVortex.Infrastructure/  # EF Core, DbContext e repositórios
│   ├── SinalVortex.API/             # Web API ASP.NET Core
│   ├── SinalVortex.Worker/          # Aplicação console para processamento
│   └── SinalVortex.slnx             # Solution file
└── frontend/
    └── app/                          # Aplicação Angular 17
        ├── src/
        ├── angular.json
        └── package.json
```

## 🚀 Requisitos

- **.NET 10** SDK
- **Node.js** 18+ com npm
- **PostgreSQL** 12+ (para o banco de dados)

## 🔧 Configuração

### Backend

1. Navegue para o diretório backend:
```bash
cd backend
```

2. Restaure as dependências:
```bash
dotnet restore
```

3. Configure a conexão com PostgreSQL no arquivo `SinalVortex.API/appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=sinalvortex;Username=postgres;Password=postgres"
  }
}
```

4. Execute as migrations (quando necessário):
```bash
dotnet ef database update -p SinalVortex.Infrastructure -s SinalVortex.API
```

### Frontend

1. Navegue para o diretório frontend:
```bash
cd frontend/app
```

2. Instale as dependências:
```bash
npm install
```

## ▶️ Executando a Aplicação

### Backend - API

```bash
cd backend
dotnet run --project SinalVortex.API/SinalVortex.API.csproj
```

A API estará disponível em: `https://localhost:5001`

### Backend - Worker

```bash
cd backend
dotnet run --project SinalVortex.Worker/SinalVortex.Worker.csproj
```

### Frontend

```bash
cd frontend/app
npm start
```

Ou use ng serve:
```bash
ng serve --open
```

A aplicação estará disponível em: `http://localhost:4200`

## 📡 Endpoints Disponíveis

### Health Check
- **GET** `/api/health` - Verifica o status da API

## 🏗️ Arquitetura

O backend segue o padrão de **Clean Architecture**:

- **Domain**: Contém as entidades e interfaces principais
- **Application**: Serviços de negócio e DTOs
- **Infrastructure**: Implementações de persistência com Entity Framework Core
- **API**: Controllers e configuração da API REST
- **Worker**: Aplicação para processamento em background

## 🗄️ Banco de Dados

O projeto usa PostgreSQL com Entity Framework Core. A string de conexão pode ser configurada em:
- `SinalVortex.API/appsettings.json` (para a API)
- `SinalVortex.Worker/appsettings.json` (para o Worker)

## 📦 Dependências Principais

### Backend
- ASP.NET Core Web API
- Entity Framework Core 10.0
- Npgsql (PostgreSQL provider)

### Frontend
- Angular 17
- Angular HTTP Client
- SCSS para estilos

## 🔗 Integração Frontend-Backend

O frontend Angular se comunica com o backend através de HTTP. A URL base da API está configurada no arquivo `HealthService`:

```typescript
private apiUrl = 'http://localhost:5000/api/health';
```

## 📝 Próximos Passos

1. Implementar entidades do domínio
2. Criar repositórios e services
3. Desenvolver endpoints adicionais
4. Criar componentes Angular conforme necessário
5. Implementar autenticação e autorização
6. Adicionar validação e tratamento de erros

## 📄 Licença

[Adicione sua licença aqui]
