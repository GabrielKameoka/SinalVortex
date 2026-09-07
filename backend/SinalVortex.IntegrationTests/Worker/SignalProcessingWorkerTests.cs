using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SinalVortex.Infrastructure.Persistence;
using StackExchange.Redis;
using System.Text.Json;
using Xunit;

namespace SinalVortex.IntegrationTests.Worker;

public class SignalProcessingWorkerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public SignalProcessingWorkerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Worker_DeveConsumirFilaDoRedis_EAtualizarStatusNoPostgreSQL()
    {
        // 1. Arrange: Obtém os serviços do Testcontainers
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var redis = scope.ServiceProvider.GetRequiredService<IConnectionMultiplexer>();
        var database = redis.GetDatabase();

        var notificacaoId = Guid.NewGuid();

        // Insere notificação pendente diretamente no PostgreSQL do container
        var sqlInsert = @"
            INSERT INTO ""Notificacoes"" (""Id"", ""AplicacaoId"", ""Destinatario"", ""Canal"", ""Prioridade"", ""Assunto"", ""Conteudo"", ""Status"", ""CriadoEm"", ""MaxTentativas"", ""Tentativas"")
            VALUES ({0}, {1}, 'worker@sinalvortex.com', 1, 1, 'Teste Worker', 'Conteudo Worker', 0, NOW(), 3, 0);";

        await dbContext.Database.ExecuteSqlRawAsync(sqlInsert, notificacaoId, Guid.NewGuid());

        var payloadFila = JsonSerializer.Serialize(new { NotificacaoId = notificacaoId });

        // 2. Act: Publica a mensagem na fila do Redis que o Worker escuta
        await database.ListLeftPushAsync("fila-notificacoes", payloadFila);

        // 3. Assert: Aguarda até 5 segundos para o Worker processar em segundo plano
        bool processado = false;
        for (int i = 0; i < 10; i++)
        {
            await Task.Delay(500);

            // Recria o scope para evitar ler dados em cache do ChangeTracker
            using var assertScope = _factory.Services.CreateScope();
            var assertDbContext = assertScope.ServiceProvider.GetRequiredService<AppDbContext>();

            var notificacao = await assertDbContext.Notificacoes
                .AsNoTracking()
                .FirstOrDefaultAsync(n => n.Id == notificacaoId);

            if (notificacao != null && notificacao.Status != 0) // 0 = Pendente
            {
                processado = true;
                break;
            }
        }

        Assert.True(processado, "O Worker não processou a mensagem da fila do Redis dentro do tempo limite.");
    }
}