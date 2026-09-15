namespace SinalVortex.Application.Dtos.Webhooks;

public enum EventoWebhook
{
    Entregue = 1,
    Falhou = 2,
    Aberto = 3,
    Clicado = 4,
    Rejeitado = 5
}

public record InboundWebhookPayloadDto(
    string Provedor,
    Guid NotificacaoId,
    EventoWebhook Evento,
    string? Detalhes,
    DateTime OcorreuEm
);