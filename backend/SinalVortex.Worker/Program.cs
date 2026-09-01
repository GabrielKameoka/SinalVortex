using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SinalVortex.Application.Common.Interfaces;
using SinalVortex.Application.Services;
using SinalVortex.Infrastructure.Persistence;
using SinalVortex.Infrastructure.Repositories;
using SinalVortex.Infrastructure.Services;
using SinalVortex.Infrastructure.Services.Notificacoes;
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
// 1. O Estado da Política de Resiliência DEVE ser Singleton
builder.Services.AddSingleton<IEmailResiliencePolicy, EmailResiliencePolicy>();

// 2. Registre as estratégias com escopo consistente (Scoped)
builder.Services.AddScoped<INotificacaoService, EmailNotificacaoService>();
builder.Services.AddScoped<INotificacaoService, SmsNotificacaoService>();
builder.Services.AddScoped<INotificacaoService, PushNotificacaoService>();
builder.Services.AddScoped<INotificacaoDispatcher, NotificacaoDispatcher>();


// MediatR
builder.Services.AddMediatR(cfg => 
    cfg.RegisterServicesFromAssembly(typeof(SinalVortex.Application.AssemblyReference).Assembly));

// Workers em Segundo Plano (Executores de Background Tasks)
builder.Services.AddHostedService<SignalProcessingWorker>();
builder.Services.AddHostedService<LimpezaNotificacoesWorker>();

var host = builder.Build();
await host.RunAsync();