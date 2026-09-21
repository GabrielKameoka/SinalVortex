# SinalVortex - Monorepo

Plataforma para processamento e despacho assíncrono de notificações com filas por prioridade, retentativas imediatas limitadas, DLQ e telemetria. E-mail SMTP e Webhook possuem transporte implementado; WhatsApp, SMS e Push ainda dependem de integração com provedores.

## Executar e investigar envios

- [Envio de e-mail local com Mailpit](docs/real-delivery.md#desenvolvimento-local-com-mailpit)
- [Investigação: notificações indo para a DLQ](docs/investigations/notificacoes-dlq.md)
- [Monitor de filas](docs/queue-monitor.md)

![CI Build](https://github.com/GabrielKameoka/SinalVortex/actions/workflows/ci.yml/badge.svg)
![License](https://img.shields.io/badge/license-MIT-blue.svg)
![.NET 10](https://img.shields.io/badge/.NET-10.0-purple.svg)
![Angular 19](https://img.shields.io/badge/Angular-19-red.svg)

## 📋 Estrutura do Projeto

```
SinalVortex/
├── backend/
│   ├── SinalVortex.Domain/          # Entidades de domínio rico e Value Objects
│   ├── SinalVortex.Application/     # Command/Query Handlers (MediatR) e DTOs
│   ├── SinalVortex.Infrastructure/  # EF Core, Redis, Repositórios e Polly
│   ├── SinalVortex.API/             # Web API RESTful (ASP.NET Core / Scalar)
│   ├── SinalVortex.Worker/          # Background Worker (Consumidor de Filas)
│   ├── SinalVortex.IntegrationTests/# Testes de Integração com Testcontainers
│   └── SinalVortex.slnx             # Solution do C#
└── frontend/app/                    # Interface de Administração (Angular 19)
```

## ⚙️ Tech Stack
- **.NET 10** (API + Worker)
- **PostgreSQL** 16 (persistência)
- **Redis 7** (filas e cache)
- **Angular 19** (frontend/admin UI)
- **Testcontainers** (integração e testes efêmeros)
- **OpenTelemetry** (telemetria e métricas)

## 🏗️ Arquitetura e Decisões Técnicas
A aplicação adota Clean Architecture e CQRS desacoplando a recepção das notificações (API) do envio real ao provedor (Worker).
```
[ Frontend Angular ] ──> [ API REST (.NET 10) ] ──> [ PostgreSQL (Persistência) ]
                                 │
                         (Enqueue Payload)
                                 ▼
                         [ Redis Queues ]
                                 │
                         (Dequeue/Process)
                                 ▼
                     [ SignalProcessingWorker ] ──> [ INotificacaoDispatcher ]
```
### Principais Decisões
- **EF Core + PostgreSQL**: Utilizado para persistência relacional auditável das notificações, logs de transição de status e gerenciamento de aplicações.
- **Redis**: Utilizado para mensageria FIFO em memória de baixa latência (separada por prioridades: Alta, Normal, Baixa e DLQ) e cache distribuído de dados de leitura frequente.
- **MediatR**: Garante desacoplamento na camada de aplicação, facilitando a inclusão de Pipeline Behaviors globais (Validação com FluentValidation e Log de Performance).

## 🛡️ Licença e Contato
Distribuído sob a licença MIT. Veja LICENSE para mais informações.
- Autor: Gabriel Mitsuru Kameoka Namazu
- GitHub: [Gabriel Kameoka](https://github.com/GabrielKameoka)
- LinkedIn: [gabrielkameoka.in](https://www.linkedin.com/in/gabriel-mitsuru/)
- E-mail: gabrielkameoka@gmail.com
