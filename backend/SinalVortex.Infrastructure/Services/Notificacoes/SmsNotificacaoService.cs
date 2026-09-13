using Microsoft.Extensions.Logging;
using SinalVortex.Application.Commands.Notificacoes;
using SinalVortex.Application.Common.Interfaces;
using SinalVortex.Domain.Enums;
using SinalVortex.Domain.Exceptions;

public class SmsNotificacaoService : INotificacaoService
{
    private readonly ILogger<SmsNotificacaoService> _logger;
    public CanalNotificacao Canal => CanalNotificacao.Sms;

    public SmsNotificacaoService(ILogger<SmsNotificacaoService> logger) => _logger = logger;

    public async Task EnviarAsync(NotificacaoFilaItemDto item, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(item.Destinatario))
            throw new PermanentChannelException("Destinatário de SMS não pode ser vazio.");

        _logger.LogInformation("[SMS Service] Enviando SMS para {Destinatario} | Conteúdo: {Conteudo}", item.Destinatario, item.Conteudo);
        await Task.Delay(50, cancellationToken);
    }
}

// PushNotificacaoService.cs
public class PushNotificacaoService : INotificacaoService
{
    private readonly ILogger<PushNotificacaoService> _logger;
    public CanalNotificacao Canal => CanalNotificacao.Push;

    public PushNotificacaoService(ILogger<PushNotificacaoService> logger) => _logger = logger;

    public async Task EnviarAsync(NotificacaoFilaItemDto item, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(item.Destinatario))
            throw new PermanentChannelException("Token do dispositivo (Push) não pode ser vazio.");

        _logger.LogInformation("[Push Service] Disparando Push Notification para {Destinatario}", item.Destinatario);
        await Task.Delay(50, cancellationToken);
    }
}