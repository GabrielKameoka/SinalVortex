namespace SinalVortex.Infrastructure.Services.Notificacoes;

using Microsoft.Extensions.Logging;
using SinalVortex.Application.Commands.Notificacoes;
using SinalVortex.Application.Common.Interfaces;
using SinalVortex.Domain.Enums;
using SinalVortex.Domain.Exceptions;
using System.Threading;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

public class WhatsappNotificacaoService : INotificacaoService
{
    private readonly ILogger<WhatsappNotificacaoService> _logger;

    public CanalNotificacao Canal => CanalNotificacao.WhatsApp;

    public WhatsappNotificacaoService(ILogger<WhatsappNotificacaoService> logger) => _logger = logger;

    public async Task EnviarAsync(NotificacaoFilaItemDto item, CancellationToken cancellationToken)
    {
        // O Value Object Destinatario normaliza telefones para somente dígitos.
        // Aceitamos também a representação E.164 (+5511...) quando o item vier
        // diretamente de uma integração, e normalizamos para o formato enviado.
        var numero = item.Destinatario?.Trim() ?? string.Empty;
        var numeroNormalizado = numero.StartsWith('+') ? numero : $"+{numero}";

        if (!Regex.IsMatch(numeroNormalizado, @"^\+\d{10,15}$"))
        {
            _logger.LogError("[WhatsApp Service] Número de telefone inválido ou fora do padrão E.164: {Destinatario}", numero);
            throw new PermanentChannelException($"Número WhatsApp inválido: {item.Destinatario}");
        }

        _logger.LogInformation("[WhatsApp Service] Disparando mensagem para {Destinatario}", numeroNormalizado);
        
        await Task.Delay(80, cancellationToken);

        _logger.LogInformation("[WhatsApp Service] Mensagem entregue com sucesso para {Destinatario}", numeroNormalizado);
    }
}
