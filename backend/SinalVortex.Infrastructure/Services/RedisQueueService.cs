using SinalVortex.Application.Common.Interfaces;
using SinalVortex.Application.Commands.Notificacoes;
using SinalVortex.Domain.Enums;
using StackExchange.Redis;

namespace SinalVortex.Infrastructure.Services;

public class RedisQueueService : IRedisQueueService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly INotificacaoRepository _notificacaoRepository;

    public RedisQueueService(IConnectionMultiplexer redis, INotificacaoRepository notificacaoRepository)
    {
        _redis = redis;
        _notificacaoRepository = notificacaoRepository;
    }

    public async Task EnfileirarNotificacaoAsync(Guid notificacaoId, PrioridadeNotificacao prioridade, CancellationToken cancellationToken = default)
    {
        var notificacao = await _notificacaoRepository.ObterPorIdAsync(notificacaoId, cancellationToken);
        if (notificacao is null)
            return;

        var db = _redis.GetDatabase();
        var queueName = ObterNomeFilaPorPrioridade(prioridade);
        var payload = new NotificacaoFilaItemDto(
            notificacao.Id,
            notificacao.AplicacaoId,
            notificacao.Canal,
            notificacao.Prioridade,
            notificacao.Destinatario.Valor,
            notificacao.Conteudo,
            notificacao.Assunto,
            notificacao.TenantId);

        await db.ListLeftPushAsync(queueName, System.Text.Json.JsonSerializer.Serialize(payload));
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
