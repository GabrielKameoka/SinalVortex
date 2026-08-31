using MediatR;
using SinalVortex.Domain.Enums;

namespace SinalVortex.Application.Commands.Notificacoes;

public record CriarNotificacaoCommand(
    Guid AplicacaoId,
    string Destinatario,
    CanalNotificacao Canal,
    PrioridadeNotificacao Prioridade,
    string Conteudo,
    string? Assunto = null,
    Guid? TemplateId = null
) : IRequest<CriarNotificacaoResultDto>;

public record CriarNotificacaoResultDto(
    Guid Id,
    StatusNotificacao Status,
    DateTime CriadoEm
);

public record NotificacaoFilaItemDto(
    Guid NotificacaoId,
    Guid AplicacaoId,
    CanalNotificacao Canal,
    PrioridadeNotificacao Prioridade,
    string Destinatario,
    string Conteudo,
    string? Assunto
);