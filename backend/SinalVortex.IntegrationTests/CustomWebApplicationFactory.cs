using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SinalVortex.Infrastructure.Persistence;
using StackExchange.Redis; // Ajuste conforme seu namespace
using Testcontainers.PostgreSql;
using Testcontainers.Redis;
using Xunit;

namespace SinalVortex.IntegrationTests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgresContainer = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("sinalvortex_test")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    private readonly RedisContainer _redisContainer = new RedisBuilder()
        .WithImage("redis:7-alpine")
        .Build();

    public async Task InitializeAsync()
    {
        // 1. Inicia os containers primeiro
        await _postgresContainer.StartAsync();
        await _redisContainer.StartAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // 2. Remove o DbContext existente registrado pelo Program.cs
            services.RemoveAll(typeof(DbContextOptions<AppDbContext>));

            // 3. Adiciona o DbContext utilizando a string de conexão DINÂMICA do Testcontainers
            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseNpgsql(_postgresContainer.GetConnectionString());
            });

            // 4. (Opcional) Sobrescreve a configuração do Redis caso utilize IDistributedCache ou StackExchange.Redis
            services.RemoveAll(typeof(IConnectionMultiplexer));
            services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(_redisContainer.GetConnectionString()));
        });
    }

    public new async Task DisposeAsync()
    {
        await _postgresContainer.StopAsync();
        await _redisContainer.StopAsync();
    }
}