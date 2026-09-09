using MediatR;
using SinalVortex.Application.Common.Interfaces;
using SinalVortex.Domain.Enums;
using SinalVortex.Domain.Exceptions;

namespace SinalVortex.Application.Commands.Notificacoes;

public record ReprocessarNotificacaoDlqCommand(Guid Id) : IRequest<bool>;

public class ReprocessarNotificacaoDlqCommandHandler : IRequestHandler<ReprocessarNotificacaoDlqCommand, bool>
{
    private readonly INotificacaoRepository _notificacaoRepository;
    private readonly IRedisQueueService _redisQueueService;

    public ReprocessarNotificacaoDlqCommandHandler(
        INotificacaoRepository notificacaoRepository,
        IRedisQueueService redisQueueService)
    {
        _notificacaoRepository = notificacaoRepository;
        _redisQueueService = redisQueueService;
    }

    public async Task<bool> Handle(ReprocessarNotificacaoDlqCommand request, CancellationToken cancellationToken)
    {
        var notificacao = await _notificacaoRepository.ObterPorIdAsync(request.Id, cancellationToken);
        if (notificacao is null)
            return false;

        // Se estiver em DLQ, aplica regra de reset
        if (notificacao.Status == StatusNotificacao.Dlq)
        {
            notificacao.ReprocessarAposDlq();
            await _notificacaoRepository.AtualizarAsync(notificacao, cancellationToken);
        }

        // Sempre re-enfileira, independente do status
        await _redisQueueService.EnfileirarNotificacaoAsync(notificacao.Id, notificacao.Prioridade, cancellationToken);

        return true;
    }

}