using MediatR;
using SinalVortex.Application.Common.Interfaces;

namespace SinalVortex.Application.Queries.Notificacoes;

public class ObterNotificacaoPorIdQueryHandler : IRequestHandler<ObterNotificacaoPorIdQuery, NotificacaoDetalhesDto?>
{
    private readonly INotificacaoRepository _notificacaoRepository;

    public ObterNotificacaoPorIdQueryHandler(INotificacaoRepository notificacaoRepository)
    {
        _notificacaoRepository = notificacaoRepository;
    }

    public async Task<NotificacaoDetalhesDto?> Handle(ObterNotificacaoPorIdQuery request, CancellationToken cancellationToken)
    {
        var notificacao = await _notificacaoRepository.ObterPorIdAsync(request.Id, cancellationToken);

        if (notificacao is null)
            return null;

        return new NotificacaoDetalhesDto(
            notificacao.Id,
            notificacao.AplicacaoId,
            notificacao.Destinatario.Valor,
            notificacao.Canal,
            notificacao.Prioridade,
            notificacao.Status,
            notificacao.Conteudo,
            notificacao.Assunto,
            notificacao.Tentativas,
            notificacao.MaxTentativas,
            notificacao.ProcessadoEm,
            notificacao.CriadoEm
        );
    }
}