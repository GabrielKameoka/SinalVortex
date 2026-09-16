using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SinalVortex.Application.Common.Interfaces;
using SinalVortex.Application.Common.Contexts;
using SinalVortex.Application.Services;
using SinalVortex.Infrastructure.Persistence;
using SinalVortex.Infrastructure.Repositories;
using SinalVortex.Infrastructure.Services;
using SinalVortex.Infrastructure.Services.Notificacoes;
using SinalVortex.Infrastructure.Services.Webhooks;
using SinalVortex.Worker;
using SinalVortex.Worker.Workers;
using StackExchange.Redis;

var builder = Host.CreateApplicationBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                       ?? "Host=localhost;Port=5432;Database=sinalvortex;Username=postgres;Password=postgres";

var redisConnectionString = builder.Configuration.GetConnectionString("RedisConnection")
                            ?? "localhost:6379";

// Banco de Dados & Caching
builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));

builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = redisConnectionString;
    options.InstanceName = "SinalVortex_";
});

builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var configuration = ConfigurationOptions.Parse(redisConnectionString);
    configuration.AbortOnConnectFail = false;
    return ConnectionMultiplexer.Connect(configuration);
});

// Registro dos Serviços da Solução
builder.Services.AddScoped<ICacheService, RedisCacheService>();
builder.Services.AddScoped<IHealthService, HealthService>();
builder.Services.AddScoped<INotificacaoRepository, NotificacaoRepository>();
builder.Services.AddScoped<ISystemNotificacaoRepository, SystemNotificacaoRepository>();
builder.Services.AddScoped<TenantContext>();
builder.Services.AddScoped<ITenantContext>(sp => sp.GetRequiredService<TenantContext>());

// Resiliência e Webhooks
builder.Services.AddSingleton<IEmailResiliencePolicy, EmailResiliencePolicy>();
builder.Services.AddSingleton<IWebhookSignatureValidator, WebhookSignatureValidator>();

// Estratégias de Notificação
builder.Services.AddScoped<INotificacaoService, EmailNotificacaoService>();
builder.Services.AddScoped<INotificacaoService, SmsNotificacaoService>();
builder.Services.AddScoped<INotificacaoService, PushNotificacaoService>();
builder.Services.AddScoped<INotificacaoService, WhatsappNotificacaoService>();
builder.Services.AddScoped<INotificacaoService, WebhookNotificacaoService>();

builder.Services.AddScoped<INotificacaoDispatcher, NotificacaoDispatcher>();

// MediatR
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(SinalVortex.Application.AssemblyReference).Assembly));

// Workers em Segundo Plano
builder.Services.AddHostedService<SignalProcessingWorker>();
builder.Services.AddHostedService<LimpezaNotificacoesWorker>();
builder.Services.AddHostedService<InboundWebhookWorker>();

var host = builder.Build();
await host.RunAsync();
