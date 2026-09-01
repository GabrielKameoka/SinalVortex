namespace SinalVortex.Infrastructure.Services.Notificacoes;

using System;
using Microsoft.Extensions.Logging;
using SinalVortex.Application.Commands.Notificacoes;
using SinalVortex.Application.Common.Interfaces;
using SinalVortex.Domain.Enums;

public class SmsNotificacaoService : INotificacaoService
{
    private readonly ILogger<SmsNotificacaoService> _logger;

    public CanalNotificacao Canal => CanalNotificacao.Sms;

    public SmsNotificacaoService(ILogger<SmsNotificacaoService> logger) => _logger = logger;

    public async Task EnviarAsync(NotificacaoFilaItemDto item, CancellationToken cancellationToken)
    {
        _logger.LogInformation("[SMS Service] Enviando SMS para: {Destinatario} | Conteúdo: {Conteudo}", item.Destinatario, item.Conteudo);
        await Task.Delay(50, cancellationToken);
    }
}