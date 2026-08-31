namespace SinalVortex.Infrastructure.Services;

using Microsoft.Extensions.Logging;
using SinalVortex.Application.Commands.Notificacoes;
using SinalVortex.Application.Common.Interfaces;
using SinalVortex.Domain.Enums;

public class NotificacaoDispatcher : INotificacaoDispatcher
{
    private readonly ILogger<NotificacaoDispatcher> _logger;

    public NotificacaoDispatcher(ILogger<NotificacaoDispatcher> logger)
    {
        _logger = logger;
    }

    public async Task EnviarAsync(NotificacaoFilaItemDto item, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Enviando notificação pelo canal [{Canal}] para {Destinatario}...", 
            item.Canal, item.Destinatario);

        await Task.Delay(300, cancellationToken);

        if (item.Destinatario.EndsWith("@error.com", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Falha de comunicação com o provedor do canal {item.Canal}.");
        }

        switch (item.Canal)
        {
            case CanalNotificacao.Email:
                _logger.LogInformation("[SinalVortex - EMAIL] Assunto: {Assunto} | Para: {Destinatario}", item.Assunto, item.Destinatario);
                break;

            case CanalNotificacao.Sms:
                _logger.LogInformation("[SinalVortex - SMS] Para: {Destinatario} | Conteúdo: {Conteudo}", item.Destinatario, item.Conteudo);
                break;

            case CanalNotificacao.WhatsApp:
                _logger.LogInformation("[SinalVortex - WHATSAPP] Para: {Destinatario} | Conteúdo: {Conteudo}", item.Destinatario, item.Conteudo);
                break;

            case CanalNotificacao.Webhook:
                _logger.LogInformation("[SinalVortex - WEBHOOK] Endpoint: {Destinatario}", item.Destinatario);
                break;

            case CanalNotificacao.Push:
                _logger.LogInformation("[SinalVortex - PUSH] Token: {Destinatario}", item.Destinatario);
                break;

            default:
                _logger.LogWarning("Canal {Canal} sem handler configurado.", item.Canal);
                break;
        }
    }
}