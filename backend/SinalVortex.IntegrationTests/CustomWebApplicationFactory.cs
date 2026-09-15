using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;
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
    private readonly PostgreSqlContainer _postgresContainer = new PostgreSqlBuilder("postgres:16-alpine")
        .WithDatabase("sinalvortex_test")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    private readonly RedisContainer _redisContainer = new RedisBuilder("redis:7-alpine")
        .Build();

    public async Task InitializeAsync()
    {
        await _postgresContainer.StartAsync();
        await _redisContainer.StartAsync();

        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseNpgsql(_postgresContainer.GetConnectionString());

        // Passa um TenantContext dummy para rodar as migrations na inicialização
        var dummyTenantContext = new SinalVortex.Application.Common.Contexts.TenantContext();
        using var context = new AppDbContext(optionsBuilder.Options, dummyTenantContext);
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
            // Subsitui/injeta o mock da interface para que o WebApplicationFactory consiga resolver a dependência
            var contatoRepoMock = Substitute.For<IContatoRepository>();
            services.AddScoped(_ => contatoRepoMock);
            
            // Fornece um TenantContext padrão para os testes de integração
            services.RemoveAll(typeof(ITenantContext));
            var testTenant = new SinalVortex.Application.Common.Contexts.TenantContext();
            testTenant.SetTenant(Guid.Parse("11111111-1111-1111-1111-111111111111"));
            services.AddSingleton<ITenantContext>(testTenant);
            
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