using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SinalVortex.Domain.Entities;
using SinalVortex.Domain.Enums;
using SinalVortex.Domain.Models;
using SinalVortex.Domain.ValueObjects;
using SinalVortex.Infrastructure.Persistence;
using StackExchange.Redis;
using Xunit;

namespace SinalVortex.IntegrationTests.Worker;

[Collection("IntegrationTestsCollection")]
public class SignalProcessingWorkerTests
{
    private readonly CustomWebApplicationFactory _factory;

    public SignalProcessingWorkerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Worker_DeveConsumirFilaDoRedis_EAtualizarStatusNoPostgreSQL()
    {
        // ARRANGE: Instancia a notificação com o ValueObject Destinatario
        var aplicacaoId = Guid.NewGuid();
        var destinatario = Destinatario.Criar("worker@sinalvortex.com", CanalNotificacao.Email);

        var notificacao = new Notificacao(
            aplicacaoId: aplicacaoId,
            destinatario: destinatario,
            canal: CanalNotificacao.Email,
            prioridade: PrioridadeNotificacao.Alta,
            conteudo: "Conteúdo para validação do processamento assíncrono",
            assunto: "Teste Worker",
            maxTentativas: 3
        );
        
        // Captura o Id gerado automaticamente pela entidade
        var notificacaoId = notificacao.Id;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.Notificacoes.AddAsync(notificacao);
            await db.SaveChangesAsync();
        }

        // Publica o payload na fila do Redis esperada pelo BackgroundWorker
        var redis = _factory.Services.GetRequiredService<IConnectionMultiplexer>();
        var redisDb = redis.GetDatabase();

        var payload = JsonSerializer.Serialize(new
        {
            NotificacaoId = notificacaoId,
            DataCriacao = DateTime.UtcNow
        });

        await redisDb.ListLeftPushAsync("fila:notificacoes", payload);

        // ACT & ASSERT (Polling): Aguarda até 5 segundos para o Worker processar
        Notificacao? notificacaoProcessada = null;
        var tempoLimite = TimeSpan.FromSeconds(5);
        var inicio = DateTime.UtcNow;

        while (DateTime.UtcNow - inicio < tempoLimite)
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            notificacaoProcessada = await db.Notificacoes
                .AsNoTracking()
                .FirstOrDefaultAsync(n => n.Id == notificacaoId);

            if (notificacaoProcessada != null && notificacaoProcessada.Status != StatusNotificacao.Pendente)
            {
                break;
            }

            await Task.Delay(200);
        }

        // Asserções Finais
        Assert.NotNull(notificacaoProcessada);
        Assert.NotEqual(StatusNotificacao.Pendente, notificacaoProcessada.Status);
        Assert.True(notificacaoProcessada.Tentativas > 0);
    }
}