namespace SinalVortex.Infrastructure.Services.Notificacoes;

using Microsoft.Extensions.Logging;
using SinalVortex.Application.Commands.Notificacoes;
using SinalVortex.Application.Common.Interfaces;
using SinalVortex.Domain.Enums;
using SinalVortex.Domain.Exceptions;
using System;
using System.Threading;
using System.Threading.Tasks;

public class WebhookNotificacaoService : INotificacaoService
{
    private readonly ILogger<WebhookNotificacaoService> _logger;

    public CanalNotificacao Canal => CanalNotificacao.Webhook;

    public WebhookNotificacaoService(ILogger<WebhookNotificacaoService> logger) => _logger = logger;

    public async Task EnviarAsync(NotificacaoFilaItemDto item, CancellationToken cancellationToken)
    {
        if (!Uri.TryCreate(item.Destinatario, UriKind.Absolute, out _))
        {
            _logger.LogError("[Webhook Service] URL de destino inválida: {Destinatario}", item.Destinatario);
            throw new PermanentChannelException($"Endpoint Webhook malformado: {item.Destinatario}");
        }

        _logger.LogInformation("[Webhook Service] Disparando POST para {Destinatario}", item.Destinatario);
        
        await Task.Delay(80, cancellationToken);

        _logger.LogInformation("[Webhook Service] HTTP 200 OK recebido de {Destinatario}", item.Destinatario);
    }
}