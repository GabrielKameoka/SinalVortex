using MediatR;
using SinalVortex.Domain.Enums;

namespace SinalVortex.Application.Queries.Notificacoes;

public record ObterNotificacaoPorIdQuery(Guid Id) : IRequest<NotificacaoDetalhesDto?>;
//Isso avisa ao MediatR: "Quando alguém me enviar (_mediator.Send), procure o Handler registrado para mim e o resultado final deve ser um NotificacaoDetalhesDto

public record NotificacaoDetalhesDto(
    Guid Id,
    Guid AplicacaoId,
    string Destinatario,
    CanalNotificacao Canal,
    PrioridadeNotificacao Prioridade,
    StatusNotificacao Status,
    string Conteudo,
    string? Assunto,
    int Tentativas,
    int MaxTentativas,
    DateTime? ProcessadoEm,
    DateTime CriadoEm
);