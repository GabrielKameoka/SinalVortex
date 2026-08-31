using MediatR;
using SinalVortex.Application.Common.Interfaces;

namespace SinalVortex.Application.Queries.Notificacoes;

public class ObterNotificacoesPaginadasQueryHandler 
    : IRequestHandler<ObterNotificacoesPaginadasQuery, PaginatedListDto<NotificacaoDetalhesDto>>
{
    private readonly INotificacaoRepository _repository;

    public ObterNotificacoesPaginadasQueryHandler(INotificacaoRepository repository)
    {
        _repository = repository;
    }

    public async Task<PaginatedListDto<NotificacaoDetalhesDto>> Handle(
        ObterNotificacoesPaginadasQuery request, 
        CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _repository.ObterPaginadoAsync(
            request.AplicacaoId,
            request.Status,
            request.Canal,
            request.PageNumber,
            request.PageSize,
            cancellationToken);

        var dtos = items.Select(n => new NotificacaoDetalhesDto(
            n.Id,
            n.AplicacaoId,
            n.Destinatario.Valor,
            n.Canal,
            n.Prioridade,
            n.Status,
            n.Conteudo,
            n.Assunto,
            n.Tentativas,
            n.MaxTentativas,
            n.ProcessadoEm,
            n.CriadoEm
        )).ToList();

        var totalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize);

        return new PaginatedListDto<NotificacaoDetalhesDto>(dtos, request.PageNumber, totalPages, totalCount);
    }
}