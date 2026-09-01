namespace SinalVortex.Infrastructure.Services.Notificacoes;

using System;
using Microsoft.Extensions.Logging;
using SinalVortex.Application.Commands.Notificacoes;
using SinalVortex.Application.Common.Interfaces;
using SinalVortex.Domain.Enums;

public class PushNotificacaoService : INotificacaoService
{
    private readonly ILogger<PushNotificacaoService> _logger;

    public CanalNotificacao Canal => CanalNotificacao.Push;

    public PushNotificacaoService(ILogger<PushNotificacaoService> logger) => _logger = logger;

    public async Task EnviarAsync(NotificacaoFilaItemDto item, CancellationToken cancellationToken)
    {
        _logger.LogInformation("[Push Service] Disparando Push Notification para token: {Destinatario}", item.Destinatario);
        await Task.Delay(50, cancellationToken);
    }
}