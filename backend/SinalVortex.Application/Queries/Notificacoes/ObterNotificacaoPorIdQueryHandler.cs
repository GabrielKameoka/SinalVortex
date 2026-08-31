namespace SinalVortex.Application.Queries.Notificacoes;

using MediatR;
using SinalVortex.Application.Common.Interfaces;

public class ObterNotificacaoPorIdQueryHandler(INotificacaoRepository notificacaoRepository) 
    : IRequestHandler<ObterNotificacaoPorIdQuery, NotificacaoDetalhesDto?>
{
    public async Task<NotificacaoDetalhesDto?> Handle(ObterNotificacaoPorIdQuery request, CancellationToken cancellationToken)
    {
        var notificacao = await notificacaoRepository.ObterPorIdAsync(request.Id, cancellationToken);

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