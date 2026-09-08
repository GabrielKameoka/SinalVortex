# ⚡ SinalVortex

> Sistema de alta performance para enfileiramento, resiliência e processamento assíncrono de notificações em massa.

[![Continuous Integration](https://github.com/gabrielmitsuru/SinalVortex/actions/workflows/ci.yml/badge.svg)](https://github.com/gabrielmitsuru/SinalVortex/actions/workflows/ci.yml)
![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet)
![Angular 17](https://img.shields.io/badge/Angular-17-DD0031?logo=angular)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-16-4169E1?logo=postgresql)
![Redis](https://img.shields.io/badge/Redis-7.0-DC382D?logo=redis)
![Docker](https://img.shields.io/badge/Docker-Containers-2496ED?logo=docker)

---

## 🎯 Sobre o Projeto

O **SinalVortex** é uma plataforma multicanal projetada sob os conceitos de **Clean Architecture** e **System Design**. Ele resolve o desafio do desacoplamento entre requisições HTTP síncronas e o envio massivo de notificações (E-mail, SMS, Push). 

As requisições recebidas pela API são validadas via padrão **CQRS** e imediatamente enfileiradas no **Redis**. Em segundo plano, o **SignalProcessingWorker** consome as filas de forma assíncrona, aplicando políticas de retentativa e registrando o ciclo de vida completo no **PostgreSQL**.

---


### Componentes do Monorepo


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

---

## 🚀 Tecnologias e Padrões Utilizados

### Backend
* **.NET 10** SDK
* **Clean Architecture** + **SOLID Principles**
* **CQRS** com MediatR
* **Entity Framework Core 10** + **Npgsql**
* **Redis** (Distribuição de Filas / Pub-Sub)
* **Testcontainers** (Ambiente de Teste Efêmero em Docker)
* **XUnit** + **FluentAssertions**

### Frontend
* **Angular 17** (Standalone Components / RxJS)
* **SCSS** modularizado

---

## 🧪 Testes de Integração & CI/CD

O projeto conta com **testes de integração automatizados e efêmeros** no projeto `SinalVortex.IntegrationTests`. 

Utilizando **Testcontainers**, a suíte de testes sobe automaticamente contêineres reais e isolados do **PostgreSQL** e **Redis** diretamente no Docker Host local ou na VM do **GitHub Actions**.

### Executando os Testes

Certifique-se de que o **Docker Desktop** (ou OrbStack) esteja em execução e rode:

```bash
cd backend
dotnet test

```

### Workflow no GitHub Actions (.github/workflows/ci.yml)

A cada `push` ou `Pull Request` enviado para as branches `main` e `develop`, a pipeline do CI executa automaticamente:

1. `dotnet restore` & `dotnet build`
2. Execução completa do `dotnet test` validando a API e o Worker contra contêineres efêmeros.

---

## 🛠️ Como Executar Localmente

### Pré-requisitos

* **.NET 10 SDK**
* **Docker** e **Docker Compose**
* **Node.js 18+** e **npm**

### 1. Subindo a Infraestrutura com Docker Compose

Na raiz do projeto, suba as instâncias de PostgreSQL e Redis:

```bash
docker-compose up -d

```

### 2. Executando o Backend

#### A. Aplicar as Migrations do Banco de Dados:

```bash
cd backend
dotnet ef database update --project SinalVortex.Infrastructure --startup-project SinalVortex.API

```

#### B. Iniciar a Web API:

```bash
dotnet run --project SinalVortex.API/SinalVortex.API.csproj

```

#### C. Iniciar o Worker Service:

Em um novo terminal:

```bash
cd backend
dotnet run --project SinalVortex.Worker/SinalVortex.Worker.csproj

```

### 3. Executando o Frontend

```bash
cd frontend/app
npm install
npm start

```

> Interface disponível em: `http://localhost:4200`

---

## 📄 Licença

Este projeto está sob a licença [MIT](https://www.google.com/search?q=./LICENSE).

```

---

<ElicitationsGroup message="Deseja realizar mais alguma melhoria na branch?">
  <Elicitation label="Criar o arquivo docker-compose.yml para a infraestrutura local" query="Forneça um exemplo de docker-compose.yml com PostgreSQL e Redis configurados para o SinalVortex."/>
  <Elicitation label="Comandos Git para commit, push e abertura do Pull Request" query="Quais comandos Git devo executar para commitar essas alterações, subir a branch feature/testcontainers-ci e abrir o PR para develop?"/>
</ElicitationsGroup>

```