using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SinalVortex.Application.Common.Interfaces;
using SinalVortex.Application.Services;
using SinalVortex.Infrastructure.Data;
using SinalVortex.Infrastructure.Services;
using SinalVortex.Worker;
using StackExchange.Redis;

var builder = Host.CreateApplicationBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
                       ?? "Host=localhost;Port=5432;Database=sinalvortex;Username=postgres;Password=postgres";

var redisConnectionString = builder.Configuration.GetConnectionString("RedisConnection") 
                            ?? "localhost:6379";

// Injeção dos Serviços
builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));

builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = redisConnectionString;
    options.InstanceName = "SinalVortex_";
});

builder.Services.AddSingleton<IConnectionMultiplexer>(sp => 
    ConnectionMultiplexer.Connect(redisConnectionString));

builder.Services.AddScoped<ICacheService, RedisCacheService>();
builder.Services.AddScoped<IHealthService, HealthService>();

// O Worker faz o mesmo registro para conseguir executar os mesmos Handlers
builder.Services.AddMediatR(cfg => 
    cfg.RegisterServicesFromAssembly(typeof(SinalVortex.Application.AssemblyReference).Assembly));

// Registra o Worker para rodar como Daemon/Serviço Contínuo
builder.Services.AddHostedService<SignalProcessingWorker>();

var host = builder.Build();
await host.RunAsync();