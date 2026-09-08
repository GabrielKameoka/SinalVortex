using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SinalVortex.Application.Common.Interfaces;
using SinalVortex.Infrastructure.Persistence;
using SinalVortex.Infrastructure.Services;
using SinalVortex.Infrastructure.Services.Notificacoes;
using SinalVortex.Worker;
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
        var redisConn = _redisContainer.GetConnectionString();
        var postgresConn = _postgresContainer.GetConnectionString();

        builder.UseSetting("ConnectionStrings:PostgreSQL", postgresConn);
        builder.UseSetting("ConnectionStrings:Redis", redisConn);
        builder.UseSetting("Redis", redisConn);
        builder.UseSetting("Redis:ConnectionString", redisConn);

        builder.ConfigureServices(services =>
        {
            // Remove qualquer registro anterior de INotificacaoService
            services.RemoveAll(typeof(INotificacaoService));

            // Registra apenas uma vez cada implementação
            services.AddScoped<INotificacaoService, EmailNotificacaoService>();
            services.AddScoped<INotificacaoService, SmsNotificacaoService>();
            services.AddScoped<INotificacaoService, PushNotificacaoService>();

            services.AddSingleton<IEmailResiliencePolicy, EmailResiliencePolicy>();

            // REGISTRA O DISPATCHER
            services.AddScoped<INotificacaoDispatcher, NotificacaoDispatcher>();

            // Worker
            services.AddHostedService<SignalProcessingWorker>();
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