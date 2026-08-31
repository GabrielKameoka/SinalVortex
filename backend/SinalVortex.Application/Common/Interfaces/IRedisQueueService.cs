namespace SinalVortex.Application.Common.Interfaces;

using SinalVortex.Domain.Enums;

public interface IRedisQueueService
{
    Task EnfileirarNotificacaoAsync(Guid notificacaoId, PrioridadeNotificacao prioridade, CancellationToken cancellationToken = default);
    Task<Guid?> DesenfileirarNotificacaoAsync(PrioridadeNotificacao prioridade, CancellationToken cancellationToken = default);
}