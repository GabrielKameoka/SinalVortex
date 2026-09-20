using SinalVortex.Application.Commands.Notificacoes;
using SinalVortex.Application.Common.Interfaces;
using SinalVortex.Domain.Enums;
using SinalVortex.Domain.Exceptions;

namespace SinalVortex.Infrastructure.Services.Notificacoes;

public sealed class PushNotificacaoService : INotificacaoService
{
    public CanalNotificacao Canal => CanalNotificacao.Push;

    public Task EnviarAsync(NotificacaoFilaItemDto item, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        throw new PermanentChannelException("Envio de Push indisponível: integração real do provedor ainda não configurada.");
    }
}
