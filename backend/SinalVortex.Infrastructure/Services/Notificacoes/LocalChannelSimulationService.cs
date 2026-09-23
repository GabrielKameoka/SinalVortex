using SinalVortex.Application.Commands.Notificacoes;
using SinalVortex.Application.Common.Interfaces;
using SinalVortex.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace SinalVortex.Infrastructure.Services.Notificacoes;

/// <summary>
/// Local-only provider used by the recruiter demo. It exercises the same queue,
/// Worker, retry/status and monitor path without contacting a paid provider.
/// Email deliberately keeps the real local SMTP/Mailpit transport.
/// </summary>
public sealed class LocalChannelSimulationService(
    CanalNotificacao canal,
    ILogger<LocalChannelSimulationService> logger) : INotificacaoService
{
    public CanalNotificacao Canal => canal;

    public async Task EnviarAsync(NotificacaoFilaItemDto item, CancellationToken cancellationToken)
    {
        await Task.Delay(TimeSpan.FromMilliseconds(180), cancellationToken);
        logger.LogInformation(
            "[Local simulator] {Canal} aceitou a notificação {Id} para {Destinatario}. Nenhuma mensagem externa foi enviada.",
            canal, item.NotificacaoId, item.Destinatario);
    }
}
