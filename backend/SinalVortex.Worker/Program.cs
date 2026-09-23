using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SinalVortex.Application.Common.Interfaces;
using SinalVortex.Application.Common.Contexts;
using SinalVortex.Application.Services;
using SinalVortex.Infrastructure.Persistence;
using SinalVortex.Infrastructure.Repositories;
using SinalVortex.Infrastructure.Services;
using SinalVortex.Infrastructure.Services.Notificacoes;
using SinalVortex.Worker;
using SinalVortex.Worker.Workers;
using StackExchange.Redis;
using SinalVortex.Infrastructure.Telemetry;
using SinalVortex.Domain.Enums;

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

// Resiliência de entrega
builder.Services.AddSingleton<IEmailResiliencePolicy, EmailResiliencePolicy>();

// Estratégias de Notificação
builder.Services.AddHttpClient("notification-webhook", client => client.Timeout = TimeSpan.FromSeconds(15))
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });
builder.Services.AddScoped<INotificacaoService, EmailNotificacaoService>();
if (builder.Environment.IsDevelopment() && builder.Configuration.GetValue<bool>("NotificationSettings:SimulateExternalChannels"))
{
    builder.Services.AddScoped<INotificacaoService>(sp => new LocalChannelSimulationService(CanalNotificacao.Sms, sp.GetRequiredService<ILogger<LocalChannelSimulationService>>()));
    builder.Services.AddScoped<INotificacaoService>(sp => new LocalChannelSimulationService(CanalNotificacao.WhatsApp, sp.GetRequiredService<ILogger<LocalChannelSimulationService>>()));
}
else
{
    builder.Services.AddScoped<INotificacaoService, SmsNotificacaoService>();
    builder.Services.AddScoped<INotificacaoService, WhatsappNotificacaoService>();
}
builder.Services.AddScoped<INotificacaoService, PushNotificacaoService>();
builder.Services.AddScoped<INotificacaoService, WebhookNotificacaoService>();

builder.Services.AddScoped<INotificacaoDispatcher, NotificacaoDispatcher>();
builder.Services.AddSingleton<QueueMonitorPublisher>();

// Workers em Segundo Plano
builder.Services.AddHostedService<SignalProcessingWorker>();
builder.Services.AddHostedService<LimpezaNotificacoesWorker>();

var host = builder.Build();

using (var scope = host.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DatabaseMigration");
    try
    {
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await context.Database.MigrateAsync();
        logger.LogInformation("Migrations do banco aplicadas com sucesso.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Não foi possível aplicar as migrations do banco na inicialização do Worker.");
    }
}

await host.RunAsync();
