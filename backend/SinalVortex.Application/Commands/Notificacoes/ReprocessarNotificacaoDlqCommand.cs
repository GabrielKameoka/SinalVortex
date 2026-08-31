using MediatR;
using SinalVortex.Application.Common.Interfaces;
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

        // Executa a regra de negócio do Domínio Rico (Valida se é DLQ, reseta tentativas para 0 e altera Status para Pendente)
        notificacao.ReprocessarAposDlq();

        // 1. Atualiza no PostgreSQL (Persiste o status Pendente e o novo LogNotificacao)
        await _notificacaoRepository.AtualizarAsync(notificacao, cancellationToken);

        // 2. Re-enfileira na Fila Redis de acordo com a prioridade da Notificação (Alta, Normal, Baixa)
        await _redisQueueService.EnfileirarNotificacaoAsync(
            notificacao.Id,
            notificacao.Prioridade,
            cancellationToken);

        return true;
    }
}