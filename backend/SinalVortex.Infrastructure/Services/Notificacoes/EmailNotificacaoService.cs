namespace SinalVortex.Infrastructure.Services.Notificacoes;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SinalVortex.Application.Commands.Notificacoes;
using SinalVortex.Application.Common.Interfaces;
using SinalVortex.Domain.Enums;
using SinalVortex.Domain.Exceptions;
using System;
using System.Threading;
using System.Threading.Tasks;

public class EmailNotificacaoService : INotificacaoService
{
    private readonly ILogger<EmailNotificacaoService> _logger;
    private readonly IConfiguration _configuration;
    private readonly IEmailResiliencePolicy _resiliencePolicy;

    public CanalNotificacao Canal => CanalNotificacao.Email;

    public EmailNotificacaoService(
        ILogger<EmailNotificacaoService> logger, 
        IConfiguration configuration,
        IEmailResiliencePolicy resiliencePolicy)
    {
        _logger = logger;
        _configuration = configuration;
        _resiliencePolicy = resiliencePolicy;
    }

    public async Task EnviarAsync(NotificacaoFilaItemDto item, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(item.Destinatario) || !item.Destinatario.Contains('@'))
        {
            _logger.LogError("[Email Service] Endereço de e-mail inválido: {Destinatario}", item.Destinatario);
            throw new PermanentChannelException($"Endereço de e-mail malformado: {item.Destinatario}");
        }

        try
        {
            await _resiliencePolicy.Pipeline.ExecuteAsync(async ct =>
            {
                await TentarEnviarPrimarioAsync(item, ct);
            }, cancellationToken);
        }
        catch (Exception ex) when (ex is not PermanentChannelException)
        {
            _logger.LogWarning(ex, "[Fallback Email] Primário falhou para {Destinatario}. Tentando secundário...", item.Destinatario);

            bool falharSecundario = _configuration.GetValue<bool>("EmailSettings:SimularFalhaSecundario");

            if (falharSecundario)
            {
                _logger.LogError("[Fallback Email] Ambos os provedores de e-mail falharam para {Destinatario}", item.Destinatario);
                throw new TransientChannelException("Ambos os provedores de e-mail estão indisponíveis.", ex);
            }

            _logger.LogInformation("[Fallback Email] Enviado via Provedor Secundário para {Destinatario}", item.Destinatario);
        }
    }

    private async Task TentarEnviarPrimarioAsync(NotificacaoFilaItemDto item, CancellationToken cancellationToken)
    {
        await Task.Delay(100, cancellationToken);

        bool simularFalhaPrimario = _configuration.GetValue<bool>("EmailSettings:SimularFalhaPrimario");

        if (simularFalhaPrimario || item.Destinatario.EndsWith("@error.com", StringComparison.OrdinalIgnoreCase))
        {
            throw new TransientChannelException("Falha de comunicação/timeout no provedor principal de e-mail.");
        }

        _logger.LogInformation("[SinalVortex - EMAIL] Enviado com sucesso. Assunto: {Assunto} | Para: {Destinatario}", item.Assunto, item.Destinatario);
    }
}