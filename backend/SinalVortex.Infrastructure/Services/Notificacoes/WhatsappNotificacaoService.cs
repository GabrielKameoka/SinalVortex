namespace SinalVortex.Infrastructure.Services.Notificacoes;

using Microsoft.Extensions.Logging;
using SinalVortex.Application.Commands.Notificacoes;
using SinalVortex.Application.Common.Interfaces;
using SinalVortex.Domain.Enums;
using SinalVortex.Domain.Exceptions;
using System.Threading;
using System.Threading.Tasks;

public class WhatsappNotificacaoService : INotificacaoService
{
    private readonly ILogger<WhatsappNotificacaoService> _logger;

    public CanalNotificacao Canal => CanalNotificacao.WhatsApp;

    public WhatsappNotificacaoService(ILogger<WhatsappNotificacaoService> logger) => _logger = logger;

    public async Task EnviarAsync(NotificacaoFilaItemDto item, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(item.Destinatario) || !item.Destinatario.StartsWith("+"))
        {
            _logger.LogError("[WhatsApp Service] Número de telefone inválido ou fora do padrão E.164: {Destinatario}", item.Destinatario);
            throw new PermanentChannelException($"Número WhatsApp inválido: {item.Destinatario}");
        }

        _logger.LogInformation("[WhatsApp Service] Disparando mensagem para {Destinatario}", item.Destinatario);
        
        await Task.Delay(80, cancellationToken);

        _logger.LogInformation("[WhatsApp Service] Mensagem entregue com sucesso para {Destinatario}", item.Destinatario);
    }
}