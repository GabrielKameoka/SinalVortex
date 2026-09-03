using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SinalVortex.Infrastructure.Persistence; // Ajuste com o seu namespace do DbContext
using Testcontainers.PostgreSql;
using Testcontainers.Redis;
using Xunit;

namespace SinalVortex.IntegrationTests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder()
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
        // 1. Sobe os containers Docker efêmeros
        await _dbContainer.StartAsync();
        await _redisContainer.StartAsync();

        // 2. Aplica as migrations do EF Core no banco criado no Testcontainers
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await dbContext.Database.MigrateAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Sobrescreve as configurações de conexões com as portas dinâmicas geradas pelos containers
        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            var customSettings = new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = _dbContainer.GetConnectionString(),
                ["ConnectionStrings:Redis"] = _redisContainer.GetConnectionString(),
                ["RedisSettings:ConnectionString"] = _redisContainer.GetConnectionString()
            };

            configBuilder.AddInMemoryCollection(customSettings);
        });
    }

    public new async Task DisposeAsync()
    {
        // Destrói os containers e libera os recursos ao finalizar os testes
        await _dbContainer.StopAsync();
        await _redisContainer.StopAsync();
    }
}