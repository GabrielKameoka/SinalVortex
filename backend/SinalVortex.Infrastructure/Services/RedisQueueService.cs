using SinalVortex.Application.Common.Interfaces;
using SinalVortex.Domain.Enums;
using StackExchange.Redis;

namespace SinalVortex.Infrastructure.Services;

public class RedisQueueService : IRedisQueueService
{
    private readonly IConnectionMultiplexer _redis;

    public RedisQueueService(IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    public async Task EnfileirarNotificacaoAsync(Guid notificacaoId, PrioridadeNotificacao prioridade, CancellationToken cancellationToken = default)
    {
        var db = _redis.GetDatabase();
        var queueName = ObterNomeFilaPorPrioridade(prioridade);
        
        await db.ListLeftPushAsync(queueName, notificacaoId.ToString());
    }

    public async Task<Guid?> DesenfileirarNotificacaoAsync(PrioridadeNotificacao prioridade, CancellationToken cancellationToken = default)
    {
        var db = _redis.GetDatabase();
        var queueName = ObterNomeFilaPorPrioridade(prioridade);
        
        var value = await db.ListRightPopAsync(queueName);

        if (value.IsNullOrEmpty)
            return null;

        return Guid.TryParse(value.ToString(), out var id) ? id : null;
    }

    private static string ObterNomeFilaPorPrioridade(PrioridadeNotificacao prioridade) => prioridade switch
    {
        PrioridadeNotificacao.Alta => "notificacoes:fila:alta",
        PrioridadeNotificacao.Normal => "notificacoes:fila:normal",
        PrioridadeNotificacao.Baixa => "notificacoes:fila:baixa",
        _ => "notificacoes:fila:normal"
    };
}