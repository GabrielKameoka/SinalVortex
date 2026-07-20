using SinalVortex.Application.Common.Interfaces;
using SinalVortex.Application.Services;
using SinalVortex.Infrastructure.Data;
using SinalVortex.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

Console.WriteLine("SinalVortex Worker iniciando...");

var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables()
    .Build();

var services = new ServiceCollection();

var connectionString = configuration.GetConnectionString("DefaultConnection") 
                       ?? "Host=localhost;Port=5432;Database=sinalvortex;Username=postgres;Password=postgres";

var redisConnectionString = configuration.GetConnectionString("RedisConnection") 
                            ?? "localhost:6379";

// Registros do DbContext
services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString)
);

// Registros do Redis no Worker
services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = redisConnectionString;
    options.InstanceName = "SinalVortex_";
});

services.AddSingleton<IConnectionMultiplexer>(sp => 
    ConnectionMultiplexer.Connect(redisConnectionString));

services.AddScoped<ICacheService, RedisCacheService>();
services.AddScoped<IHealthService, HealthService>();

var serviceProvider = services.BuildServiceProvider();

// Teste de conexão/uso do Redis no Worker
using (var scope = serviceProvider.CreateScope())
{
    var cacheService = scope.ServiceProvider.GetRequiredService<ICacheService>();
    await cacheService.SetAsync("worker_status", "Worker operacional com Redis!", TimeSpan.FromMinutes(5));
    
    var status = await cacheService.GetAsync<string>("worker_status");
    Console.WriteLine($"[Redis Cache Test]: {status}");
}

Console.WriteLine("Worker finalizado");