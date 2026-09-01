namespace SinalVortex.Infrastructure.Services.Notificacoes;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SinalVortex.Application.Commands.Notificacoes;
using SinalVortex.Application.Common.Interfaces;
using SinalVortex.Domain.Enums;
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
        try
        {
            // Tenta pelo pipeline do Polly (Provedor Principal + Circuit Breaker)
            await _resiliencePolicy.Pipeline.ExecuteAsync(async ct =>
            {
                await TentarEnviarPrimarioAsync(item, ct);
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("[Fallback Email] Primário/Circuit Breaker falhou: {Msg}. Redirecionando...", ex.Message);

            bool falharSecundario = _configuration.GetValue<bool>("EmailSettings:SimularFalhaSecundario");

            if (falharSecundario)
            {
                _logger.LogError("[Fallback Email] Provedor Secundário TAMBÉM falhou.");
                throw new InvalidOperationException("Falha catastrófica: Ambos os provedores de e-mail estão indisponíveis.", ex);
            }

            _logger.LogInformation("[Fallback Email] Enviado com sucesso via Provedor Secundário.");
        }
    }

    private async Task TentarEnviarPrimarioAsync(NotificacaoFilaItemDto item, CancellationToken cancellationToken)
    {
        await Task.Delay(100, cancellationToken);

        bool simularFalhaPrimario = _configuration.GetValue<bool>("EmailSettings:SimularFalhaPrimario");

        if (simularFalhaPrimario || item.Destinatario.EndsWith("@error.com", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Falha de comunicação com o provedor principal de e-mail.");
        }

        _logger.LogInformation("[SinalVortex - EMAIL] Assunto: {Assunto} | Para: {Destinatario}", item.Assunto, item.Destinatario);
    }
}