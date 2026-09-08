using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SinalVortex.Infrastructure.Persistence;
using StackExchange.Redis;
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
        // 1. Inicia os contêineres do Testcontainers
        await _postgresContainer.StartAsync();
        await _redisContainer.StartAsync();

        // 2. Aplica as Migrations uma única vez no container efêmero
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseNpgsql(_postgresContainer.GetConnectionString());

        using var context = new AppDbContext(optionsBuilder.Options);
        await context.Database.MigrateAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // 3. Substitui o DbContext do app pelo contexto apontando para a porta dinâmica do PostgreSQL
            services.RemoveAll(typeof(DbContextOptions<AppDbContext>));
            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseNpgsql(_postgresContainer.GetConnectionString());
            });

            // 4. Substitui a conexão do Redis apontando para a porta dinâmica do RedisContainer
            services.RemoveAll(typeof(IConnectionMultiplexer));
            services.AddSingleton<IConnectionMultiplexer>(
                ConnectionMultiplexer.Connect(_redisContainer.GetConnectionString())
            );
        });
    }

    public new async Task DisposeAsync()
    {
        await _postgresContainer.StopAsync();
        await _redisContainer.StopAsync();
    }
}

// Collection Definition para garantir execução sequencial dos testes de integração no xUnit
[CollectionDefinition("IntegrationTestsCollection")]
public class IntegrationTestCollection : ICollectionFixture<CustomWebApplicationFactory>
{
}