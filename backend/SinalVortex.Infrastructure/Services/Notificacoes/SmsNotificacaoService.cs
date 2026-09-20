using SinalVortex.Application.Commands.Notificacoes;
using SinalVortex.Application.Common.Interfaces;
using SinalVortex.Domain.Enums;
using SinalVortex.Domain.Exceptions;

namespace SinalVortex.Infrastructure.Services.Notificacoes;

public sealed class SmsNotificacaoService : INotificacaoService
{
    public CanalNotificacao Canal => CanalNotificacao.Sms;

    public Task EnviarAsync(NotificacaoFilaItemDto item, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        throw new PermanentChannelException("Envio de Sms indisponível: integração real do provedor ainda não configurada.");
    }
}
