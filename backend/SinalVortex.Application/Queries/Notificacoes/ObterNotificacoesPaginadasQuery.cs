using MediatR;
using SinalVortex.Domain.Enums;

namespace SinalVortex.Application.Queries.Notificacoes;

public record ObterNotificacoesPaginadasQuery(
    Guid? AplicacaoId,
    StatusNotificacao? Status,
    CanalNotificacao? Canal,
    int PageNumber = 1,
    int PageSize = 10
) : IRequest<PaginatedListDto<NotificacaoDetalhesDto>>;

public record PaginatedListDto<T>(
    IReadOnlyCollection<T> Items,
    int PageNumber,
    int TotalPages,
    int TotalCount
)
{
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;
}