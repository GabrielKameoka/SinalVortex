namespace SinalVortex.Application.Commands.Webhooks;

using MediatR;
using SinalVortex.Application.Common.Interfaces;
using SinalVortex.Application.Dtos.Webhooks;
using SinalVortex.Domain.Enums;

public record ProcessarInboundWebhookCommand(InboundWebhookPayloadDto Payload) : IRequest<bool>;

public class ProcessarInboundWebhookCommandHandler : IRequestHandler<ProcessarInboundWebhookCommand, bool>
{
    private readonly INotificacaoRepository _notificacaoRepository;

    public ProcessarInboundWebhookCommandHandler(INotificacaoRepository notificacaoRepository)
    {
        _notificacaoRepository = notificacaoRepository;
    }

    public async Task<bool> Handle(ProcessarInboundWebhookCommand request, CancellationToken cancellationToken)
    {
        var dto = request.Payload;
        var notificacao = await _notificacaoRepository.ObterPorIdAsync(dto.NotificacaoId, cancellationToken);

        if (notificacao is null)
            return false;

        switch (dto.Evento)
        {
            case EventoWebhook.Entregue:
                if (notificacao.Status == StatusNotificacao.EmProcessamento || notificacao.Status == StatusNotificacao.Pendente)
                {
                    notificacao.MarcarComoEnviado();
                }
                break;

            case EventoWebhook.Falhou:
            case EventoWebhook.Rejeitado:
                if (notificacao.Status == StatusNotificacao.EmProcessamento || notificacao.Status == StatusNotificacao.Pendente)
                {
                    notificacao.RegistrarFalha(dto.Detalhes ?? "Falha informada via Webhook do provedor.");
                }
                break;
        }

        await _notificacaoRepository.AtualizarAsync(notificacao, cancellationToken);
        return true;
    }
}